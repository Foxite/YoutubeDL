using YoutubeExplode.Videos;

namespace YoutubeDL {
	public abstract class DownloadResult {
		public abstract bool Success { get; }
		public abstract string Message { get; }
		public abstract string NotificationChannel { get; }
		public string VideoTitle { get; }
		public VideoId Video { get; }

		protected DownloadResult(VideoId video, string videoTitle) {
			Video = video;
			VideoTitle = videoTitle;
		}
	}
}