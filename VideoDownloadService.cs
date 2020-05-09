using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace YoutubeDL {
	[Service]
	public class VideoDownloadService : Service {
		private YoutubeClient m_Client;

		public override IBinder OnBind(Intent intent) {
			return new BinderImpl(this);
		}

		public override void OnCreate() {
			base.OnCreate();
			m_Client = new YoutubeClient();
		}

		public override void OnDestroy() {
			base.OnDestroy();
			m_Client = null;
		}

		internal async Task<DownloadResult> DownloadVideo(string youtubeUrl, IProgress<double> progress) {
			var videoId = new VideoId(youtubeUrl);
			Video video = null;
			try {
				video = await m_Client.Videos.GetAsync(videoId);
				var audioStream = (await m_Client.Videos.Streams.GetManifestAsync(videoId)).GetAudioOnly().Where(info => info.Container == Container.Mp4).WithHighestBitrate();

				if (audioStream != null) {
					string fileName = Path.Combine(Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath, Util.SanitizeFilename(video.Title) + ".mp3");

					await m_Client.Videos.Streams.DownloadAsync(audioStream, fileName, progress);

					return new SuccessfulResult(videoId, video.Title);
				} else {
					return new NoStreamsResult(videoId, video.Title);
				}
			} catch (Exception e) {
				Log.Error(Util.LogTag, Java.Lang.Throwable.FromException(e), "Exception when trying to download video " + youtubeUrl);
				return new ExceptionResult(videoId, video?.Title);
			}
		}

		#region Binder
		public abstract class Binder : Android.OS.Binder {
			public VideoDownloadService Service { get; }

			protected Binder(VideoDownloadService service) {
				Service = service;
			}
		}

		// Restricts instantation to VideoDownloadService class
		private class BinderImpl : Binder {
			public BinderImpl(VideoDownloadService service) : base(service) { }
		}
		#endregion

		#region Service connection
		public class Connection : Java.Lang.Object, IServiceConnection {
			public Binder Binder { get; private set; }

			public void OnServiceConnected(ComponentName name, IBinder binder) {
				Binder = (Binder) binder;
			}

			public void OnServiceDisconnected(ComponentName name) {
				Binder = null;
			}
		}
		#endregion
	}
}
