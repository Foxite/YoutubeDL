using YoutubeExplode.Videos;

namespace YoutubeDL {
	public class SuccessfulResult : DownloadResult {
		public override bool Success => true;
		public override string NotificationChannel => "youtubedl.finished";
		public override string Message => "@string/notif_title_finished";

		public string PathToFile { get; }

		public SuccessfulResult(VideoId video, string videoTitle, string pathToFile) : base(video, videoTitle) {
			PathToFile = pathToFile;
		}
	}
}