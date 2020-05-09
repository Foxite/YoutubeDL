using System;
using System.Diagnostics;
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

namespace YoutubeDL {
	[Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true)]
	[IntentFilter(
		new[] { Intent.ActionSend },
		Categories = new[] { Intent.CategoryDefault },
		DataHosts = new[] { "youtube.com", "youtu.be" },
		DataMimeType = "text/plain")]
	public class MainActivity : AppCompatActivity {
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
				int notificationID = s_NextNotificationID++;

				// Don't run this on the main thread
				Task.Run(async () => {
					try {
						var manager = this.GetSystemService<NotificationManager>();

						var notifBuilder = new NotificationCompat.Builder(ApplicationContext, "youtubedl.download")
							.SetSmallIcon(Resource.Mipmap.ic_launcher)
							.SetContentText("@string/notif_pre_start")
							.SetProgress(100, 0, true);

						manager.Notify(notificationID, notifBuilder.Build());

						VideoDownloadService.Connection connection = new VideoDownloadService.Connection();
						Intent serviceIntent = new Intent(ApplicationContext, typeof(VideoDownloadService));
						StartService(serviceIntent);
						bool bindResult = BindService(serviceIntent, connection, Bind.AutoCreate);

						await Task.Delay(500);
						DownloadResult result = await connection.Binder.Service.DownloadVideo(youtubeUrl, new Progress<double>(p => {
							notifBuilder.SetProgress(100, (int) (p * 100), false);
							manager.Notify(notificationID, notifBuilder.SetOngoing(true).Build());
						}));

						UnbindService(connection);
						//StopService(serviceIntent); // ?

						manager.Notify(
							notificationID,
							new NotificationCompat.Builder(ApplicationContext, result.NotificationChannel)
								.SetSmallIcon(Resource.Mipmap.ic_launcher)
								.SetContentTitle(result.VideoTitle ?? result.Video.Value)
								.SetOngoing(false)
								.SetContentText(result.Message)
								.Build()
						);
					} catch (Exception e) {
						Log.Error(Util.LogTag, e.ToStringDemystified());
					}
				});
				//Finish();
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
