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
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Support.V7.App;
using Android.Views;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using Environment = System.Environment;

namespace YoutubeDL {
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
	[IntentFilter(
		new[] { Intent.ActionSend },
		Categories = new[] { Intent.CategoryDefault },
		DataHosts = new[] { "youtube.com", "youtu.be" },
		DataMimeType = "text/plain")]
	public class MainActivity : AppCompatActivity {
		protected override void OnCreate(Bundle savedInstanceState) {
			base.OnCreate(savedInstanceState);
			Xamarin.Essentials.Platform.Init(this, savedInstanceState);
			SetContentView(Resource.Layout.activity_main);

			Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
			SetSupportActionBar(toolbar);

			FloatingActionButton fab = FindViewById<FloatingActionButton>(Resource.Id.fab);
			fab.Click += FabOnClick;

			if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != (int) Permission.Granted) {
				ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.WriteExternalStorage }, 0);
			}

			if (ContextCompat.CheckSelfPermission(this, Manifest.Permission.ReadExternalStorage) != (int) Permission.Granted) {
				ActivityCompat.RequestPermissions(this, new string[] { Manifest.Permission.ReadExternalStorage }, 0);
			}

			if (Intent?.Extras != null) {
				string youtubeUrl = Intent.GetStringExtra(Intent.ExtraText);
				Task.Run(async () => {
					try {
						var client = new YoutubeClient();
						var video = await client.Videos.GetAsync(new VideoId(youtubeUrl));
						AudioOnlyStreamInfo audioStream = (await client.Videos.Streams.GetManifestAsync(new VideoId(youtubeUrl))).GetAudioOnly().First();

						string fileName = Path.Combine(
							Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath,
							video.Title + "." + audioStream.Container
						);

						var notif = new NotificationCompat.Builder(base.ApplicationContext, "youtubedl.progress")
							.SetContentTitle(video.Title)
							.SetProgress(0, 100, false)
							.SetSmallIcon(Resource.Mipmap.ic_launcher);

						var manager = (NotificationManager) GetSystemService(Java.Lang.Class.FromType(typeof(NotificationManager)));
						await client.Videos.Streams.DownloadAsync(audioStream, fileName, new DownloadProgress(manager, notif));
						manager.Cancel(1337);
					} catch (Exception e) {
						System.Diagnostics.Debugger.Break();
					}
				});
			}
		}

		private class DownloadProgress : IProgress<double> {
			private readonly NotificationManager m_Manager;
			private readonly NotificationCompat.Builder m_Builder;

			public DownloadProgress(NotificationManager manager, NotificationCompat.Builder builder) {
				m_Manager = manager;
				m_Builder = builder;
				m_Manager.Notify(1337, m_Builder.Build());
			}

			public void Report(double progress) {
				m_Builder.SetProgress(100, (int) progress * 100, false);
				m_Manager.Notify(1337, m_Builder.Build());
			}
		}

		public override bool OnCreateOptionsMenu(IMenu menu) {
			MenuInflater.Inflate(Resource.Menu.menu_main, menu);
			return true;
		}

		public override bool OnOptionsItemSelected(IMenuItem item) {
			int id = item.ItemId;
			if (id == Resource.Id.action_settings) {
				return true;
			}

			return base.OnOptionsItemSelected(item);
		}

		private void FabOnClick(object sender, EventArgs eventArgs) {
			View view = (View) sender;
			Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
				.SetAction("Action", (Android.Views.View.IOnClickListener) null).Show();
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults) {
			Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

			base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
		}
	}
}

