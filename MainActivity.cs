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
			base.OnCreate(savedInstanceState);
			Xamarin.Essentials.Platform.Init(this, savedInstanceState);
			SetContentView(Resource.Layout.activity_main);

			Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
			SetSupportActionBar(toolbar);

			EnsurePermissions();

			if (Intent?.Extras != null) {
				string youtubeUrl = Intent.GetStringExtra(Intent.ExtraText);
				// Don't run this on the main thread
				Task.Run(() => DownloadVideo(youtubeUrl));
				Finish();
			}
		}

		private void EnsurePermissions() {
			if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != (int) Permission.Granted) {
				ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.WriteExternalStorage }, 0);
			}

			if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != (int) Permission.Granted) {
				ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ReadExternalStorage }, 0);
			}
		}

		private async Task DownloadVideo(string youtubeUrl) {
			int notificationID = s_NextNotificationID++;
			var manager = this.GetSystemService<NotificationManager>();
			var videoId = new VideoId(youtubeUrl);

			void makeNotif(string title, int stringID, string channel) =>
				// Doesn't work in emulator.
				// Works fine in real device.
				manager.Notify(notificationID,
					new NotificationCompat.Builder(ApplicationContext, channel)
						.SetContentTitle(title)
						.SetContentText(Resources.GetString(stringID))
						.SetSmallIcon(Resource.Mipmap.ic_launcher)
						.Build()
				);

			try {
				var client = new YoutubeClient();
				var video = await client.Videos.GetAsync(videoId);
				var audioStream = (await client.Videos.Streams.GetManifestAsync(videoId)).GetAudioOnly().Where(info => info.Container == Container.Mp4).WithHighestBitrate();

				if (audioStream != null) {
					string fileName = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath, Util.SanitizeFilename(video.Title) + ".mp3");

					var notif = new NotificationCompat.Builder(base.ApplicationContext, "youtubedl.progress")
						.SetContentTitle(video.Title)
						.SetProgress(0, 100, false)
						.SetSmallIcon(Resource.Mipmap.ic_launcher);

					await client.Videos.Streams.DownloadAsync(audioStream, fileName, new Progress<double>(p => {
						notif.SetProgress(100, (int) (p * 100), false);
						manager.Notify(notificationID, notif.Build());
					}));

					makeNotif(video.Title, Resource.String.notif_title_finished, "youtubedl.finished");
				} else {
					makeNotif(video.Title, Resource.String.notif_title_no_streams, "youtubedl.failed.nostreams");
				}
			} catch (Exception e) {
				Log.Error(LogTag, Java.Lang.Throwable.FromException(e), "Exception when trying to download video " + videoId.Value);
				makeNotif(videoId.Value, Resource.String.notif_title_exception, "youtubedl.failed.exception");
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
	}
}
