using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Util;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDL {
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar")]
	[IntentFilter(
		new[] { Intent.ActionSend },
		Categories = new[] { Intent.CategoryDefault },
		DataHosts = new[] { "youtube.com", "youtu.be" },
		DataMimeType = "text/plain")]
	public class MainActivity : Activity {
		private const string LogTag = "FOXITE_YOUTUBE_DL";

		private static int s_NextNotificationID;

		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);
			Xamarin.Essentials.Platform.Init(this, savedInstanceState);

			string[] requiredPermissions = new string[] { Manifest.Permission.Internet, Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage };
			if (requiredPermissions.Any(perm => ContextCompat.CheckSelfPermission(this, perm) != Permission.Granted)) {
				ActivityCompat.RequestPermissions(this, requiredPermissions, 0);
			} else {
				DoDownload();
			}
		}

		private void DoDownload() {
			Finish();
			// Major threaded spaghetti below.
			if (Intent?.Extras != null) {
				string youtubeUrl = Intent.GetStringExtra(Intent.ExtraText);

				Task.Run(async () => {
					int notificationID = s_NextNotificationID++;
					var manager = (NotificationManager) GetSystemService(Java.Lang.Class.FromType(typeof(NotificationManager)));
					if (Build.VERSION.SdkInt >= BuildVersionCodes.O) {
						manager.CreateNotificationChannel(new NotificationChannel("youtubedl", "YoutubeDL", NotificationImportance.Low));
					}

					var videoId = new VideoId(youtubeUrl);

					NotificationCompat.Builder notif = new NotificationCompat.Builder(ApplicationContext, "youtubedl")
						.SetProgress(0, 100, true)
						.SetOngoing(true)
						.SetSound(null)
						.SetSmallIcon(Resource.Mipmap.ic_launcher);

					void makeNotif(string title, string text) {
						manager.Notify("Download", notificationID, notif
							.SetContentTitle(title)
							.SetContentText(text)
							.Build()
						);
					}

					var rwls = new ReaderWriterLockSlim();
					double progress = 0;

					try {
						var client = new YoutubeClient();
						var video = await client.Videos.GetAsync(videoId);

						makeNotif(video.Title, "Downloading");

						var audioStream = (await client.Videos.Streams.GetManifestAsync(videoId)).GetAudioOnly().Where(info => info.Container == Container.Mp4).WithHighestBitrate();
						notif.SetContentTitle(video.Title);

						if (audioStream != null) {
							string fileName = Path.Combine(
								Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath,
								SanitizeFilename(video.Title) + ".mp3"
							);

							Task notifLoop = Task.Run(async () => {
								while (true) {
									rwls.EnterReadLock();
									if (progress == -1) {
										rwls.ExitReadLock();
										return;
									} else if (progress >= 1) {
										notif.SetProgress(0, 0, false);
										notif.SetOngoing(false);
										makeNotif(video.Title, "Finished downloading");
										rwls.ExitReadLock();
										return;
									} else {
										notif.SetProgress(100, (int) (progress * 100), false);
										manager.Notify("Download", notificationID, notif.Build());
										notif.SetOngoing(true);
										rwls.ExitReadLock();
									}
									await Task.Delay(TimeSpan.FromMilliseconds(500));
								}
							});

							await client.Videos.Streams.DownloadAsync(audioStream, fileName, new Progress<double>(p => {
								try {
									rwls.EnterWriteLock();
									if (progress != -1) {
										progress = p;
									}
								} finally {
									rwls.ExitWriteLock();
								}
							}));

							rwls.EnterWriteLock();
							progress = 1;
							rwls.ExitWriteLock();

							await notifLoop;
						} else {
							makeNotif(video.Title, "This video cannot be downloaded. A future update may fix this.");
						}
					} catch (Exception e) {
						rwls.EnterWriteLock();
						Log.Error(LogTag, Java.Lang.Throwable.FromException(e), "Exception when trying to download video " + videoId.Value);
						notif.SetProgress(0, 0, false);
						notif.SetOngoing(false);
						progress = -1;
						makeNotif(e.GetType().Name, "Cannot download video because of an unknown error. Trying again may fix the problem. If this persists, contact the developer, and include a link to the video you downloaded.");
						rwls.ExitWriteLock();
					}
				});
			}
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			DoDownload();
		}

		private string SanitizeFilename(string inputStr) {
			char[] input = inputStr.ToCharArray();
			char[] invalidChars = Path.GetInvalidFileNameChars();
			for (int i = 0; i < input.Length; i++) {
				if (invalidChars.Contains(input[i])) {
					input[i] = '_';
				}
			}
			return new string(input);
		}
	}
}
