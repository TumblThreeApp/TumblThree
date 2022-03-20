<img align="left" width="60" height="60" src="https://raw.githubusercontent.com/wiki/TumblThreeApp/TumblThree/images/tumblthree_icon.png" alt="TumblThree Logo">

# TumblThree - A Tumblr Blog Backup Application

[![Build status](https://ci.appveyor.com/api/projects/status/dbrmr06nm3jif5bd/branch/master?svg=true)](https://ci.appveyor.com/project/TumblThreeApp/tumblthree/branch/master)
[![GitHub All Releases (archived repo)](https://img.shields.io/github/downloads/johanneszab/TumblThree/total?label=downloads%20%28archived%20repo%29&style=social)](https://github.com/johanneszab/TumblThree)
[![Github Releases (current repo)](https://img.shields.io/github/downloads/TumblThreeApp/TumblThree/total.svg?style=flat)](https://github.com/TumblThreeApp/TumblThree/releases)

[![first-timers-only](https://img.shields.io/badge/first--timers--only-friendly-blue.svg?style=flat)](https://www.firsttimersonly.com/)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg?style=flat)](http://makeapullrequest.com)
[![contributions welcome](https://img.shields.io/badge/contributions-welcome-brightgreen.svg?style=flat)](https://github.com/TumblThreeApp/TumblThree/issues)

TumblThree is a free and open source Tumblr and Twitter blog backup application. It downloads photo, video, audio and text posts from a given tumblr or twitter blog. [![Tweet](https://img.shields.io/twitter/url/http/shields.io.svg?style=social)](https://twitter.com/intent/tweet?text=Check%20out%20TumblThree%20-%20A%20Tumblr%20and%20Twitter%20Blog%20Backup%20Application%0AIt%20downloads%20photo,%20video,%20audio%20and%20text%20posts%20from%20a%20given%20blog.&url=https%3A%2F%2Ftumblthreeapp.github.io&hashtags=tumblr,blog,backup,application)<br/>
It is the code rewrite of [TumblTwo](https://github.com/johanneszab/TumblTwo), using the [Win Application Framework (WAF)](https://github.com/jbe2277/waf) and C# with WPF and the MVVM pattern.

<img valign="middle" src="https://raw.githubusercontent.com/wiki/TumblThreeApp/TumblThree/images/label_new_yellow.png" alt="new" height="30"/>&nbsp;&nbsp; TumblThree 2.0 - Now with Twitter blog downloader!

## Features

* Source code at github (Written in C# using WPF and MVVM).
* Multiple concurrent downloads of a single blog.
* Multiple concurrent downloads of different blogs.
* Internationalization support (several languages available).
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
* Change the blog settings of multiple selected blogs at once
* Uses SSL connections.
* Preview of photos & videos.
* Taskbar buttons and key bindings.
* File rename functionality
* :star: Download of Twitter blogs 🆕
* Image viewer with slideshow mode 🆕
* Automated update process 🆕
* Group blogs into collections 🆕

### Blog backup/download:

* Download of photo, video (only tumblr.com hosted), text, audio, quote, conversation, link and question posts.
* Download meta information for photo, video and audio posts.
* Downloads inlined photos and videos (e.g. photos embedded in question&answer posts).
* Download of all image sizes possible (SVC, API only for newer blogs, higher resolution not possible for old blogs)
* Support for downloading Imgur, Gfycat, Webmshare, Uguu and Catbox linked files in tumblr posts.
* Download of safe mode/NSFW blogs.
* Allows to download only original content of the blog and skip reblogged posts.
* Can download only tagged posts.
* Can download only specific blog pages instead of the whole blog.
* Allows to download blog posts in a defined time span.
* Can download hidden blogs (login required / dash board blogs).
* Can download password protected blogs (of non-hidden blogs).

### Liked/by and Likes backup/download:

* A downloader for downloading "liked by" (e.g. https://www.tumblr.com/liked/by/wallpaperfx/) and "likes" (e.g. https://www.tumblr.com/likes) photos and videos instead of a tumblr blog (login required).
* Download of all image sizes possible (SVC, API only for newer blogs, higher resolution not possible for old blogs)
* Allows to download posts in a defined time span. 

### Tumblr search backup/download:

* A downloader for downloading photos and videos from the tumblr search (e.g. http://www.tumblr.com/search/my+keywords).
* Download of all image sizes possible (SVC, API only for newer blogs, higher resolution not possible for old blogs)
* Allows to download posts in a defined time span.

### Tumblr tag search backup/download:

* A downloader for downloading photos and videos from the tumblr tag search (e.g. http://www.tumblr.com/tagged/my+keywords) (login required).
* Download of all image sizes possible (SVC, API only for newer blogs, higher resolution not possible for old blogs)
* Allows to download posts in a defined time span.

### 🆕 Twitter Blog backup/download:

* Download of photo, video and text posts of (public) blogs.
* Download meta information for photo and video posts.
* Allows to download only original content of the blog and skip reblogged posts.
* Can download only tagged posts.
* Allows to download blog posts in a defined time span.

## Download

Latest versions can be found [here](https://github.com/TumblThreeApp/TumblThree/releases).

Please keep in mind that probably only the latest version is functioning properly since the platforms evolve and from time to time change their data structures which makes changes in TumblThree necessary again. So update your application regularly.

## Screenshot
![TumblThree Main UI](https://raw.githubusercontent.com/wiki/TumblThreeApp/TumblThree/images/tumblthree.png "TumblThree Main UI")

## Application Usage

Read our wiki page about [Application Usage](https://github.com/TumblThreeApp/TumblThree/wiki/How-to-use-the-Application).

## Getting Started

The default settings should cover most users. You should only have to change the download location and the kind of posts you want to download. You can find more information in our wiki [Getting Started](https://github.com/TumblThreeApp/TumblThree/wiki/Getting-Started) and [Insights](https://github.com/TumblThreeApp/TumblThree/wiki/Insights).

## Feedback and Bug reports

If you like TumblThree, give it a star <img src="https://raw.githubusercontent.com/wiki/TumblThreeApp/TumblThree/images/star.png" alt="star" height="16"/> (at the right upper corner of the page)!

We appreciate it, if you send us your feedback or file a bug report. Preferably, just fill out an [issue form](https://github.com/TumblThreeApp/TumblThree/issues/new/choose). If you don't have a GitHub account yet, please sign up for one, it is free.

In case you don't like to register an account for some reason, but still want to provide feedback or a bug report, use the following web form:
[tumblthreeapp.github.io/TumblThree/feedback.html](https://tumblthreeapp.github.io/TumblThree/feedback.html)

## How to Build the Source Code to Help Further Development

* Download [Visual Studio](https://www.visualstudio.com/vs/community/). The minimum required version is Visual Studio 2015 (C# 6.0 feature support).
* Download the [source code as .zip file](https://github.com/TumblThreeApp/TumblThree/archive/master.zip) or use a [git client](https://git-scm.com/download/gui/windows) (e.g. [GitHub Desktop](https://desktop.github.com/) or [TortoiseGit](https://tortoisegit.org/)) and [checkout the code](https://github.com/TumblThreeApp/TumblThree.git).
* Open the TumblThree.sln solution file in the src/ directory of the code.
* Build the Source once before editing anything. Build->Build Solution.

## Contributing to TumblThree

We like the [all-contributors](https://allcontributors.org/) specification. Contributions of any kind are welcome!
If you've ever wanted to contribute to open source, and a great cause, now is your chance!

* You can find useful information about How and What you can contribute to the TumblThree project [here](Contributing.md).
* Also see the [wiki page for ideas of new or missing features](https://github.com/TumblThreeApp/TumblThree/wiki/New-Feature-Requests-and-Possible-Enhancements) and add your thoughts.

## Contributors ✨

Last but not least see also the [list of contributors](docs/Contributors.md) who participated in this project.
