using YoutubeExplode.Videos;

namespace YoutubeDL {
	public class SuccessfulResult : DownloadResult {
		public override bool Success => true;
		public override string NotificationChannel => "youtubedl.finished";
		public override string Message => "@string/notif_title_finished";

		public SuccessfulResult(VideoId video, string videoTitle) : base(video, videoTitle) { }
	}
}