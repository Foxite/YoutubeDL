using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDL {
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
	[IntentFilter(
		new[] { Intent.ActionSend },
		Categories = new[] { Intent.CategoryDefault },
		DataHosts = new[] { "youtube.com", "youtu.be" },
		DataMimeType = "text/plain")]
	public class MainActivity : AppCompatActivity {
		private const string LogTag = "FOXITE_YOUTUBE_DL";

		private static int s_NextNotificationID;

		protected override void OnCreate(Bundle savedInstanceState) {
			// TODO planned features:
			// - History in main activity
			// - Configure download location
			// - Configure download type (support all types available)
			// - Move hardcoded strings to strings.xml
			base.OnCreate(savedInstanceState);
			Xamarin.Essentials.Platform.Init(this, savedInstanceState);
			SetContentView(Resource.Layout.activity_main);

			Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
			SetSupportActionBar(toolbar);

			if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != (int) Permission.Granted) {
				ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.WriteExternalStorage }, 0);
			}

			if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != (int) Permission.Granted) {
				ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ReadExternalStorage }, 0);
			}

			if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.Internet) != (int) Permission.Granted) {
				ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.Internet }, 0);
			}

			if (Intent?.Extras != null) {
				string youtubeUrl = Intent.GetStringExtra(Intent.ExtraText);
				Finish(); // doesn't work
				Task.Run(async () => {
					int notificationID = s_NextNotificationID++;
					var manager = (NotificationManager) GetSystemService(Java.Lang.Class.FromType(typeof(NotificationManager)));
					manager.CreateNotificationChannel(new NotificationChannel("youtubedl", "YoutubeDL", NotificationImportance.Default));

					var videoId = new VideoId(youtubeUrl);

					NotificationCompat.Builder notif = new NotificationCompat.Builder(base.ApplicationContext, "youtubedl")
						.SetProgress(0, 100, false)
						.SetSmallIcon(Resource.Mipmap.ic_launcher);

					void makeNotif(string title, string text) {
						manager.Notify(notificationID, notif
							.SetContentTitle(title)
							.SetContentText(text)
							.Build()
						);
					}

					try {
						var client = new YoutubeClient();
						var video = await client.Videos.GetAsync(videoId);
						var audioStream = (await client.Videos.Streams.GetManifestAsync(videoId)).GetAudioOnly().Where(info => info.Container == Container.Mp4).WithHighestBitrate();
						notif.SetContentTitle(video.Title);

						if (audioStream != null) {
							string fileName = Path.Combine(
								Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath,
								SanitizeFilename(video.Title) + ".mp3"
							);

							await client.Videos.Streams.DownloadAsync(audioStream, fileName, new Progress<double>(p => {
								notif.SetProgress(100, (int) (p * 100), false);
								manager.Notify(notificationID, notif.Build());
							}));

							await Task.Delay(1000); // Hack because it only works half the time, for some reason

							notif.SetProgress(0, 0, false);
							makeNotif(video.Title, "Finished downloading");
						} else {
							makeNotif(video.Title, "This video cannot be downloaded. A future update may fix this.");
						}
					} catch (Exception e) {
						Log.Error(LogTag, Java.Lang.Throwable.FromException(e), "Exception when trying to download video " + videoId.Value);
						makeNotif(e.GetType().Name, "Cannot download video because an unknown error. Trying again may fix the problem. If this persists, contact the developer, and include a link to the video you downloaded.");
					}
				});
			}
		}

		public override bool OnCreateOptionsMenu(IMenu menu) {
			MenuInflater.Inflate(Resource.Menu.menu_main, menu);
			return true;
		}

		public override bool OnOptionsItemSelected(IMenuItem item) {
			int id = item.ItemId;
			if (id == Resource.Id.action_settings) {
				// Open the options menu here
				return true;
			}

			return base.OnOptionsItemSelected(item);
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
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
