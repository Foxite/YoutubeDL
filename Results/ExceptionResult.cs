using System;
using YoutubeExplode.Videos;

namespace YoutubeDL {
	public class ExceptionResult : DownloadResult {
		public override bool Success => false;
		public override string NotificationChannel => "youtubedl.failed.exception";
		public override string Message => "@string/notif_title_exception";

		public Exception Exception { get; }

		public ExceptionResult(VideoId video, string videoTitle, Exception exception) : base(video, videoTitle) {
			Exception = exception;
		}
	}
}