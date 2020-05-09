using YoutubeExplode.Videos;

namespace YoutubeDL {
	public class NoStreamsResult : DownloadResult {
		public override bool Success => false;
		public override string NotificationChannel => "youtubedl.failed.nostreams";
		public override string Message => "@string/notif_title_no_streams";

		public NoStreamsResult(VideoId video, string videoTitle) : base(video, videoTitle) { }
	}
}