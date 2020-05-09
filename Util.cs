using System;
using System.IO;
using System.Linq;
using Android.Content;

namespace YoutubeDL {
	public static class Util {
		public static T GetSystemService<T>(this Context context) where T : Java.Lang.Object {
			return (T) context.GetSystemService(Java.Lang.Class.FromType(typeof(T)));
		}

		public static string SanitizeFilename(string unsanitizedPath) {
			char[] path = unsanitizedPath.ToCharArray();
			char[] illegalCharacters = Path.GetInvalidFileNameChars();
			for (int i = 0; i < path.Length; i++) {
				if (illegalCharacters.Contains(path[i])) {
					path[i] = '_';
				}
			}
			return new string(path);
		}
	}
}