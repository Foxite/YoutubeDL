# YoutubeDL
An android app using Xamarin that quickly downloads youtube videos as MP3. It uses [YoutubeExplode](https://github.com/Tyrrrz/YoutubeExplode) to achieve this.

Not affiliated with the popular command line tool.

## Installation
This app will likely never be released to Google Play. For an explanation, try to look up "youtube downloader" on the play store, and count the results you get. However, release on F-Droid is [pending](https://gitlab.com/fdroid/rfp/-/issues/1445) (currently waiting on Xamarin support, which is not high on the todo list).

In the meantime, you can [download a released apk](https://github.com/Foxite/YoutubeDL/releases). To install those, you need to enable downloading apps from unknown sources. You may consult a search engine if you don't know how to do this.

Alternatively, get it on a temporary, non-official F-Droid-compatible repository.

[<img src="https://gitlab.com/IzzyOnDroid/repo/-/raw/master/assets/IzzyOnDroid.png" width="170">](https://apt.izzysoft.de/fdroid/index/apk/nl.dirkkok.android.youtubedl)

## Instructions
Once installed, usage is very simple:

1. Open the Youtube app and share a video to YoutubeDL (actually, you should be able to share any youtube link from anywhere, but I haven't tested this).
2. If this your first time using the app, grant permission to access the internal storage and the internet.
3. The main activity will close (if everything worked out). At this point you can continue what you were doing, including sharing additional videos. After a few seconds a notification will pop up with the download progress. When finished, the mp3 will be placed in your downloads folder on the external storage.

## Disclaimer
This app was originally built in about 4 hours, purely for my convenience. Per the license, there is no guarantee that any part of it actually works. If this app fucks your phone, I will not be responsible.

This app is not monetized in any way.
