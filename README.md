<img align="left" width="60" height="60" src="https://user-images.githubusercontent.com/71355143/97790805-1e444100-1bcc-11eb-90c7-bafda041bf94.png" alt="TumblThree Logo">

# TumblThree - A Tumblr Blog Backup Application

[![Build status](https://ci.appveyor.com/api/projects/status/dbrmr06nm3jif5bd/branch/master?svg=true)](https://ci.appveyor.com/project/TumblThreeApp/tumblthree/branch/master)
[![GitHub All Releases (archived repo)](https://img.shields.io/github/downloads/johanneszab/TumblThree/total?label=downloads%20%28archived%20repo%29&style=social)](https://github.com/johanneszab/TumblThree)
[![Github Releases (current repo)](https://img.shields.io/github/downloads/TumblThreeApp/TumblThree/total.svg?style=flat)](https://github.com/TumblThreeApp/TumblThree/releases)

[![first-timers-only](https://img.shields.io/badge/first--timers--only-friendly-blue.svg?style=flat)](https://www.firsttimersonly.com/)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat)](http://makeapullrequest.com)

TumblThree is the code rewrite of [TumblTwo](https://github.com/johanneszab/TumblTwo), a free and open source Tumblr blog backup application, using C# with WPF and the MVVM pattern. It uses the [Win Application Framework (WAF)](https://github.com/jbe2277/waf). It downloads photo, video, audio and text posts from a given tumblr blog.

_Read this in other languages: [简体中文](README.zh-cn.md)._

## Features:

* Source code at github (Written in C# using WPF and MVVM).
* Multiple concurrent downloads of a single blog.
* Multiple concurrent downloads of different blogs.
* Internationalization support (currently available: zh, ru, de, fr, es).
* A download queue.
* Autosave of the queuelist.
* Save, clear and restore the queuelist.
* A clipboard monitor that detects *blogname.tumblr.com* urls in the clipboard (copy and paste) and automatically adds the blog to the bloglist.
* A settings panel (change download location, turn preview off/on, define number of concurrent downloads, set the imagesize of downloaded pictures, set download defaults, enable portable mode, etc.).
* Uses Windows proxy settings.
* A bandwidth throttler.
* An option to download an url list instead of the actual files.
* Set a start time for a automatic download (e.g. during nights).
* An option to skip the download of a file if it has already been downloaded before in any currently added blog.
* Uses SSL connections.
* Preview of photos & videos.
* Taskbar buttons and key bindings.

### Blog backup/download:

* Download of photo, video (only tumblr.com hosted), text, audio, quote, conversation, link and question posts.
* Download meta information for photo, video and audio posts.
* Downloads inlined photos and videos (e.g. photos embedded in question&answer posts).
* ~~Download of \_raw image files (original/higher resolution pictures)~~ [(Tumblr raws are inaccessible as of August 10, 2018)](https://github.com/johanneszab/TumblThree/issues/261).
* Support for downloading Imgur, Gfycat, Webmshare, Mixtape, Lolisafe, Uguu, Catbox and SafeMoe linked files in tumblr posts.
* Download of safe mode/NSFW blogs.
* Allows to download only original content of the blog and skip reblogged posts.
* Can download only tagged posts.
* Can download only specific blog pages instead of the whole blog.
* Allows to download blog posts in a defined time span.
* Can download hidden blogs (login required / dash board blogs).
* Can download password protected blogs (of non-hidden blogs).

### Liked/by backup/download:

* A downloader for downloading "liked by" photos and videos instead of a tumblr blog (e.g. https://www.tumblr.com/liked/by/wallpaperfx/) (login required).
* ~~Download of \_raw image files (original/higher resolution pictures)~~ [(Tumblr raws are inaccessible as of August 10, 2018)](https://github.com/johanneszab/TumblThree/issues/261).
* Allows to download posts in a defined time span. 

### Tumblr search backup/download:

* A downloader for downloading photos and videos from the tumblr search (e.g. http://www.tumblr.com/search/my+keywords).
* ~~Download of \_raw image files (original/higher resolution pictures)~~ [(Tumblr raws are inaccessible as of August 10, 2018)](https://github.com/johanneszab/TumblThree/issues/261). 
* Can download only specific blog pages instead of the whole blog.

### Tumblr tag search backup/download:

* A downloader for downloading photos and videos from the tumblr tag search (e.g. http://www.tumblr.com/tagged/my+keywords) (login required).
* ~~Download of \_raw image files (original/higher resolution pictures)~~ [(Tumblr raws are inaccessible as of August 10, 2018)](https://github.com/johanneszab/TumblThree/issues/261). 
* Allows to download posts in a defined time span.

## Download:

Latest releases can be found [here](https://github.com/TumblThreeApp/TumblThree/releases).

*If you experience crashes right before or while logging in, it may be that on your system the "[Visual C++ Redistributable for Visual Studio 2015](https://www.microsoft.com/en-us/download/details.aspx?id=48145)" is missing. You can download it from MS.*

## Screenshot:
![TumblThree Main UI](http://www.jzab.de/sites/default/files/images/tumblthree.png?raw=true "TumblThree Main UI")

## Application Usage:

Read our wiki page about [Application Usage](https://github.com/TumblThreeApp/TumblThree/wiki/How-to-use-the-Application)

## Getting Started:

The default settings should cover most users. You should only have to change the download location and the kind of posts you want to download. You can find more information in our wiki [Getting Started](https://github.com/TumblThreeApp/TumblThree/wiki/Getting-Started).

## Limitations and Further Insights:

* The old datasets from TumblTwo and TumblOne are __not__ compatible.
* No more support for Windows XP.

More information about TumblThree can be found in our wiki [Insights](https://github.com/TumblThreeApp/TumblThree/wiki/Insights).
 
## How to Build the Source Code to Help Further Developing:

* Download [Visual Studio](https://www.visualstudio.com/vs/community/). The minimum required version is Visual Studio 2015 (C# 6.0 feature support).
* Download the [source code as .zip file](https://github.com/TumblThreeApp/TumblThree/archive/master.zip) or use the [GitHub Desktop](https://desktop.github.com/) and [checkout the code](https://github.com/TumblThreeApp/TumblThree.git).
* Open the TumblThree.sln solution file in the src/ directory of the code.
* Build the Source once before editing anything. Build->Build Solution.

## Translations wanted:

* If you want to help translate TumblThree, there are two resource files (.resx) which contain all the strings used in the application. One for [the user interface](https://github.com/TumblThreeApp/TumblThree/blob/master/src/TumblThree/TumblThree.Presentation/Properties/Resources.resx#L120) and one for the [underlying application](https://github.com/TumblThreeApp/TumblThree/blob/master/src/TumblThree/TumblThree.Applications/Properties/Resources.resx#L120).  
* Translate all the words or its meanings between the two value tags and create a pull request on github or simply send us the files via email.
 
## Contributing to TumblThree:

We like the [all-contributors](https://allcontributors.org/) specification. Contributions of any kind are welcome!
If you've ever wanted to contribute to open source, and a great cause, now is your chance!

* You can find useful information about How and What you can contribute to the TumblThree project [here](docs/Contributing.md).
* Also see the [wiki page for ideas of new or missing features](https://github.com/TumblThreeApp/TumblThree/wiki/New-Feature-Requests-and-Possible-Enhancements) and add your thoughts.

## Contributors ✨

Last but not least see also the list of [contributors](docs/Contributors.md) who participated in this project.
