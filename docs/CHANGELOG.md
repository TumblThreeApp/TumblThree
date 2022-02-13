# Changelog

All notable changes to this project will be documented in this file.
 
The format is based on [Keep a Changelog](http://keepachangelog.com/)
and this project adheres to [Semantic Versioning](http://semver.org/).

Please keep in mind that with great certainty older versions will not
run anymore because bugs have been fixed and data structures are 
changing over time.

<br>

## 2.5.1 (2022-02-06)

#### Changed
- Setting "Display confirmation dialogs" is now on by default (issue 210)

#### Fixed
- Some Twitter videos are downloaded incomplete since last version
<br>

## 2.5.0 (2022-01-31)

#### Added
- Removing a queue item stops its crawler

#### Fixed
- Downloaded file is huge compared to actual image data (issue #207)
- Limit exceeded in Twitter crawler (issue #209)
- More files are downloaded than intended (issue #207)
- Remove too much crawl overhead
- Blog url textbox layout
- Changed translation
<br>

## 2.4.8 (2022-01-14)

#### Fixed
- Show correct state and login name after login/logout ([#192](https://github.com/TumblThreeApp/TumblThree/issues/192))
- Error when saving settings after app language has been changed
- Main window shown in wrong language after app language change ([#202](https://github.com/TumblThreeApp/TumblThree/issues/202))
- JSON files use brackets now ([#200](https://github.com/TumblThreeApp/TumblThree/issues/200))
- Not all archived blogs are loaded for duplicate check
- Creation of additional (unused) archive folder in collection's download location
<br>

## 2.4.7 (2022-01-06)

#### Fixed
- Status message not showing login name ([#192](https://github.com/TumblThreeApp/TumblThree/issues/192))
- In some cases only 100 posts are downloaded ([#174](https://github.com/TumblThreeApp/TumblThree/issues/174))
- Problem with collections handling
- Problem downloading some older, smaller Twitter blogs
- Exceptions when entering path names with illegal characters
	
#### Removed
- Separate zip files for the translations(*)
	
**Notes:** From this version on there will be no longer a separate zip file for the translations, they are included in the application zip file now.
If you are still using the 32-bit version (x86), please verify whether you can also use the 64-bit version on your system, if not, please give us a feedback.
<br>

## 2.4.6 (2021-12-23)
	
#### Changed
- Use concurrent scans in Liked/By crawler
	
#### Fixed
- Couldn't download blogs with custom domain containing hyphen ([#195](https://github.com/TumblThreeApp/TumblThree/issues/195))
- Gifv not downloading ([#197](https://github.com/TumblThreeApp/TumblThree/issues/197))
- Liked/By crawler not working ([#196](https://github.com/TumblThreeApp/TumblThree/issues/196))
- Inconsistent date/time values used for API/SVC crawler ([#197](https://github.com/TumblThreeApp/TumblThree/issues/197))
- Inlined tumblr video has wrong filename/date ([#197](https://github.com/TumblThreeApp/TumblThree/issues/197))
- API crawler saves image meta information with wrong time ([#197](https://github.com/TumblThreeApp/TumblThree/issues/197))
<br>

## 2.4.5 (2021-12-13)

#### Fixed
- Chosen collection folder not shown immediately
- The argument %b in filename template ([PR #194](https://github.com/TumblThreeApp/TumblThree/pull/194))
- 'Refresh' is not allowed during an AddNew or EditItem transaction
- Status message not updating after login ([#192](https://github.com/TumblThreeApp/TumblThree/issues/192))
<br>

## 2.4.4 (2021-12-04)

#### Changed
- Change the way the update package is determined
- User agent string

#### Fixed
- Adjust Twitter post title for file rename template
- Tumblr Search not working while logged out ([#190](https://github.com/TumblThreeApp/TumblThree/issues/190))
- Error when selecting blog with non-default collection assigned
<br>

## 2.4.3 (2021-11-20)

#### Changed
- Options to download audio and text in Tumblr Search

#### Fixed
- Error "'Refresh' is not allowed during an AddNew or EditItem transaction"
- Tumblr Search doesn't work any more
<br>

## 2.4.2 (2021-11-13)

#### Changed
- Write additional information into the log file on startup

#### Fixed
- Error "Specified cast is not valid"
- Two folders are created when adding a new blog ([#178](https://github.com/TumblThreeApp/TumblThree/issues/178))
- For some error types the blog name isn't shown in message
- Possible error when adding blogs through clipboard monitor
- Error when archiving removed blog if blog was already removed before
- Liked-by crawler not downloading all posts ([#187](https://github.com/TumblThreeApp/TumblThree/issues/187))
<br>

## 2.4.1 (2021-11-01)

#### Changed
- Ability to assign collections to existing blogs ([#170](https://github.com/TumblThreeApp/TumblThree/issues/170))
- Send version number with feedback

#### Fixed
- Error if Twitter blog doesn't exist
- Video posts with non-tumblr embedded videos lead to files with html content
- Error "'Refresh' is not allowed during an AddNew or EditItem transaction"
- Ensure a new blog uses a new folder
- Setting for User-Agent does not persist ([#177](https://github.com/TumblThreeApp/TumblThree/issues/177))
- Tumblr (Tag) Search not working
<br>

## 2.4.0 (2021-10-17)

#### Added
- Feedback button in About dialog

#### Fixed
- Error ItemsControl inconsistent ([#170](https://github.com/TumblThreeApp/TumblThree/issues/170))
- Can't download files in original size ([#171](https://github.com/TumblThreeApp/TumblThree/issues/171))
- Cannot close settings dialog with default collection selected
- FormatExceptions when opening image viewer
<br>

## 2.3.0 (2021-10-10)

#### Added
- Ability to add different collections ([#170](https://github.com/TumblThreeApp/TumblThree/issues/170))

#### Fixed
- Tag search crawler not working any more
<br>

## 2.2.1 (2021-10-03)

#### Fixed
- Some twitter videos were downloaded incomplete ([#169](https://github.com/TumblThreeApp/TumblThree/issues/169))
<br>

## 2.2.0 (2021-09-22)

#### Added
- Automated update process ([#143](https://github.com/TumblThreeApp/TumblThree/issues/143))

#### Fixed
- Full-screen preview doesn't follow the blogs
- Possible error while loading image viewer

*Note: The zip files no longer contain a root folder.*

<br>

## 2.1.0 (2021-09-11)

#### Added
- Image viewer with slideshow mode

#### Fixed
- Check blogs from all subfolders of the archive folder
- Save dump data json files only once for photo sets with default filenames
- Add license files to application zip archive
- Do not create backups in archive folder
- Preview not updating even other blogs are downloaded ([#150](https://github.com/TumblThreeApp/TumblThree/issues/150))
<br>

## 2.0.1 (2021-08-07)

#### Fixed
- Don't archive blog files while switching blog type during adding a blog
- Problem to download Tumblr hidden blog ([#162](https://github.com/TumblThreeApp/TumblThree/issues/162))
<br>

## 2.0.0 (2021-07-24)

**TumblThree 2.0 - Now with Twitter blog downloader!**

#### Added
- A Twitter crawler for downloading (public) Twitter blogs ([#161](https://github.com/TumblThreeApp/TumblThree/issues/161))
<br>

## 1.6.5 (2021-07-20)

#### Fixed
- Tumblr Search crawler skips rest of page's posts if one fails
- Error KeyNotFoundException on start of next crawl after the crawler type was changed
- Possible error when downloading post in (Tag) Search crawler
- Prevent possible error during authentication
<br>

## 1.6.4 (2021-07-06)

#### Changed
- Selection of multiple blogs shows inconsistent detail values visibly to prevent accidental overwrites

#### Fixed
- Blogs sometimes do not change their color after their downloads have completed
- Privacy consent message can lead to exception
<br>

## 1.6.3 (2021-06-21)

#### Fixed
- Possible errors in crawler
- Optimization of restore database entries from local disc on forced rescans
- Possible error while loading blog databases ([#159](https://github.com/TumblThreeApp/TumblThree/issues/159))
- Possible error in downloader
- Possible error during logout
- Some crawler dump files missing due to incorrect json generation
<br>

## 1.6.2 (2021-06-13)

#### Fixed
- A post from blog was not parsable ([#157](https://github.com/TumblThreeApp/TumblThree/issues/157))
- Parsing error in search blogs
- Speed up database item existence checks
- Liked-By crawler not downloading files ([#158](https://github.com/TumblThreeApp/TumblThree/issues/158))
<br>

## 1.6.1 (2021-06-10)

#### Fixed
- Download of high resolution images failed ([#157](https://github.com/TumblThreeApp/TumblThree/issues/157))
- Possible error adding a new blog
- Fixing crawler error handlers
<br>

## 1.6.0 (2021-06-02)

#### Added
- New token for blog name inside filename pattern
- Extend file rename functionality for inline/generic media of a post
- LoadArchive / ArchiveIndex options in settings dialog

#### Fixed
- Crash if blog index/db files not found ([#119](https://github.com/TumblThreeApp/TumblThree/issues/119))
- Missing downloads when using %d token ([#146](https://github.com/TumblThreeApp/TumblThree/issues/146))
- Several minor errors
<br>

## 1.5.2 (2021-05-20)

#### Fixed
- Change of the language could produce an error on next app start
- The logging of an error shortly after app start could lead to a crash
- App not starting after update to newer version ([#147](https://github.com/TumblThreeApp/TumblThree/issues/147)/[#148](https://github.com/TumblThreeApp/TumblThree/issues/148)/[#151](https://github.com/TumblThreeApp/TumblThree/issues/151))
- More detailed information in case of errors
<br>

## 1.5.1 (2021-05-16)

#### Added
- Some more translations
- Language selection in settings dialog

#### Fixed
- Problem in global exception handling
- Threading and timeout error handling
- Some minor errors
<br>

## 1.5.0 (2021-05-14)

#### Added
- Download of personal tumblr likes ([#20](https://github.com/TumblThreeApp/TumblThree/issues/20))

#### Fixed
- Show wait cursor while loading databases
- Better global exception handling
<br>

## 1.4.1 (2021-04-23)

#### Fixed
- Wrong calculation of downloaded items ([#135](https://github.com/TumblThreeApp/TumblThree/issues/135))
- File rename functionality ([#18](https://github.com/TumblThreeApp/TumblThree/issues/18))
- Added MS VC++ Redistributable package check
- Prevent some more app crashes ([#144](https://github.com/TumblThreeApp/TumblThree/issues/144))
- Improved global exception handling
<br>

## 1.4.0 (2021-04-01)

#### Added
- Blogs with custom domain can be added and downloaded ([#101](https://github.com/TumblThreeApp/TumblThree/issues/101))
- Color cues for the error message bar ([#124](https://github.com/TumblThreeApp/TumblThree/issues/124))

#### Fixed
- Save inlined (HTML-embedded) media with post date as modification timestamp ([#111](https://github.com/TumblThreeApp/TumblThree/issues/111))
<br>

## 1.3.1 (2021-03-19)

#### Fixed
- Logout functionality
- New column with timestamp of latest post ([#103](https://github.com/TumblThreeApp/TumblThree/issues/103))
- Cleanup after fail adding blog
- Problem with blog online status check
- Tumblr Search Offline ([#130](https://github.com/TumblThreeApp/TumblThree/issues/130))
<br>

## 1.3.0 (2021-03-04)

#### Added
- Reminder to download new update version
- Download posts from Tumblr Search results with sort order "recent" ([#125](https://github.com/TumblThreeApp/TumblThree/issues/125))

#### Fixed
- Changing multiple blogs causes crash ([#123](https://github.com/TumblThreeApp/TumblThree/issues/123))
- Some minor UI flaws
- Search crawler with new privacy consent handling ([#125](https://github.com/TumblThreeApp/TumblThree/issues/125))
<br>

## 1.2.0 (2021-02-07)

#### Added
- Rework of Save photo sets with similar filenames ([#56](https://github.com/TumblThreeApp/TumblThree/issues/56)/[#104](https://github.com/TumblThreeApp/TumblThree/issues/104))
- Automatically save the blog settings after leaving the details pane
- Option "Download url list" renamed to "Save url list" ([#121](https://github.com/TumblThreeApp/TumblThree/issues/121))
- File rename functionality ([#18](https://github.com/TumblThreeApp/TumblThree/issues/18))

#### Fixed
- Error starting the next item in the queue ([#120](https://github.com/TumblThreeApp/TumblThree/issues/120))
- Possible errors during Liked By and Tag Search crawling
<br>

## 1.1.0 (2021-01-18)

#### Added
- Downloading blog's own reblogs ([#91](https://github.com/TumblThreeApp/TumblThree/issues/91))
- Save photo sets with similar filenames ([#56](https://github.com/TumblThreeApp/TumblThree/issues/56)/[#104](https://github.com/TumblThreeApp/TumblThree/issues/104))

#### Fixed
- Sometimes app stops downloading blogs ([#34](https://github.com/TumblThreeApp/TumblThree/issues/34))
<br>

## 1.0.12.2 (2021-01-10)

#### Added
- Detect previously crawled posts earlier
- Better statistic updates for non-full crawls
- Preview follows the crawled blogs ([#117](https://github.com/TumblThreeApp/TumblThree/issues/117))

#### Fixed
- Locking error collection for thread safety
- Improved http error handling
- Sometimes app stops downloading blogs ([#34](https://github.com/TumblThreeApp/TumblThree/issues/34))
<br>

## 1.0.12.1 (2021-01-01)

#### Fixed
- Tag search download
- Logout functionality
- Prevent overwrite of changed Location and ChildId settings
- Authenticate/Login dialog (white window problem) ([#115](https://github.com/TumblThreeApp/TumblThree/issues/115))
- Blocking of download queue

#### Added
- New column with timestamp of latest post ([#103](https://github.com/TumblThreeApp/TumblThree/issues/103))
<br>

## 1.0.12.0 (2020-12-19)

TumblThree (Xmas Release)

#### Fixed
- Download of high resolution images in API mode ([issue 261](https://github.com/johanneszab/TumblThree/issues/261))

#### Added
- Possibility of settings upgrades
<br>

## 1.0.11.13 (2020-12-02)

Updated the dependency [CefSharp.Wpf](https://github.com/cefsharp/cefsharp) from 85.3.130 to 86.0.241. See CefSharp's [Release notes](https://github.com/cefsharp/CefSharp/releases/tag/v86.0.241) for details.
Critical bugs were found in the dependency [CefSharp.Wpf](https://github.com/cefsharp/cefsharp) and they provided critical security updates ([CVE-2020-16013](https://github.com/advisories/GHSA-x7fx-mcc9-27j7), [CVE-2020-16017](https://github.com/advisories/GHSA-gvqv-779r-4jgp) and CVE-2020-16009).
As we only use this browser control for the login process, it shouldn't have been any great security risk.

#### Fixed
- Prevent NotImplementedExceptions during download ([#99](https://github.com/TumblThreeApp/TumblThree/issues/99))
- Let the 32bit app download the correct update zip file again ([#100](https://github.com/TumblThreeApp/TumblThree/issues/100))

#### Added
- Periodically save the blog's database for reliability ([#26](https://github.com/TumblThreeApp/TumblThree/issues/26))
<br>

## 1.0.11.12 (2020-10-27)

Updated the dependency [CefSharp.Wpf](https://github.com/cefsharp/cefsharp) from 84.4.10 to 85.3.130.
See CefSharp's [Release notes](https://github.com/cefsharp/CefSharp/releases/tag/v85.3.130) for details.

A critical bug was found in the dependency [CefSharp.Wpf](https://github.com/cefsharp/cefsharp) and they provided a critical security update.
See [Heap overflow in the freetype library (CVE-2020-15999)](https://github.com/cefsharp/CefSharp/security/advisories/GHSA-pv36-h7jh-qm62) for details.

As we only use this browser control for the login process, it shouldn't have been any great security risk.

<br>

## 1.0.11.11 (2020-10-05)

#### Fixed
- add delimiting commas between objects when saving blog metadata as JSON ([#82](https://github.com/TumblThreeApp/TumblThree/issues/82))
- Liked/by crawler can get stuck in an endless loop ([#92](https://github.com/TumblThreeApp/TumblThree/issues/92))

#### Added
- basic logging functionality, choose level in general settings ([#21](https://github.com/TumblThreeApp/TumblThree/issues/21))
<br>

## 1.0.11.10 (2020-09-28)

No features added or bugs fixed.
This release just provides a 32-bit version again.

<br>

## 1.0.11.9 (2020-09-26)

#### Fixed
- Broken login due to changed privacy consent agreement

To login, go to Settings->Connections, press the Authenticate button. An embedded browser window will open with the Tumblr login page opened. After logging in using the password method, also perform the new privacy consent agreement. When you're back on the Tumblr start page, simply close the browser window.

This TumblThree release only supports 64-bit windows.

See issue [#81](https://github.com/TumblThreeApp/TumblThree/issues/81) for more.

<br>

## 1.0.11.8 (2020-08-12)

Fixes Tumblr Searches

Re-implements Tumblr Searches:

* E.g. https://www.tumblr.com/tagged/cars to search for "car" tagged posts, and
* E.g. https://www.tumblr.com/search/cars to search for "car" posts.

See [#75](https://github.com/TumblThreeApp/TumblThree/issues/75) for more.

<br>

## 1.0.11.7 (2020-07-19)

Use standard .NET TLS version instead of OS TLS version. 

This prevents TLS (SChannel 70, EventID 36887) errors and hence unsuccessful connections on Windows versions pre Windows 10. For example, the standard TLS version of Windows 7 is TLS 1.0 which is deprecated.

The settings in this commit sets the TLS version to the standard TLS version of the currently installed .NET Framework (4.7.X) instead of the System/OS default TLS version.

For more see the issue [#74](https://github.com/TumblThreeApp/TumblThree/issues/74). Thanks a lot to @KyleC69 and @mamift and everyone else in that issue for figuring this one out!

<br>

## 1.0.11.6 (2020-06-25)

Cache StreamWriter instances for text downloading

Stores StreamWriter instances in Dictionary and reuses them for recurring text appends in text post downloading.
This prevents massive seek I/O in large blog downloads, and hence poor (disk) performance ([#72](https://github.com/TumblThreeApp/TumblThree/issues/72)).

<br>

## 1.0.11.5 (2020-02-18)

Cumulative Improvements

* Allows to add [tumbex urls](https://www.tumbex.com/) via GUI, text, or clipboard ([#50](https://github.com/TumblThreeApp/TumblThree/issues/50)).
* Can open blogs on tumbex.com via the context menu (right mouse click, [#50](https://github.com/TumblThreeApp/TumblThree/issues/50)).
* Can download tumblr photos with non-"tumblr_"-prefix. Thanks to @ShadowBlade72 for fixing this ([#47](https://github.com/TumblThreeApp/TumblThree/issues/47), [#58](https://github.com/TumblThreeApp/TumblThree/issues/58)).
* You can now choose which tumblr blog scraper you want to use:
  * Tumblr API: The previous default crawler for non-hidden blogs which utilized the Tumblr v1 API, or
  * Tumblr SVC: This service is (was?) used by Tumblr internally for displaying hidden blogs. Using this scraper requires a to be logged in.

  You can change the crawler in the details view of each blogs. Using the SVC crawler implementation might be faster if it's not as much rate limited as accessing the site via the official v1 API. For more information, see [#46](https://github.com/TumblThreeApp/TumblThree/issues/46).

* Can download higher resolution images if available using the SVC crawler. For this, change all your tumblr blogs to use the SVC crawler via the Details Panel -> Crawler -> "Tumblr SVC". Set the downloadable image size in the Settings (Settings->Connections) to "best". This will download the highest resolution image found ([#51](https://github.com/TumblThreeApp/TumblThree/issues/51)).

* It's now possible to set a separate rate limit for the SVC crawler in settings->connections.

* It's now possible to set the default crawler for Tumblr Blogs in the settings in settings->blog. You can choose between "Tumblr API" und "Tumblr SVC". If you do not tick this checkbox, the default automatic detection will add the blog depending on if it's accessible via the Tumblr API. If it's not, then the SVC crawler will be used as it requires to be logged in (see the notes above for more information on which to pick).

* Fixes clipboard monitor toggle button which was defunct (i.e. always active) after the awesome font icon migration.

* Saves the settings directly after performing changes instead of only on application exit ([#61](https://github.com/TumblThreeApp/TumblThree/issues/61)).

* Fixes crashes on empty / offline Tumblr Liked-by pages or when crawling using the Tumblr SVC Crawler ([#63](https://github.com/TumblThreeApp/TumblThree/issues/63))."
<br>

## 1.0.10.1008 (2019-03-22)

Bugfix release

#### Fixed

- Bug present when attempting to download some images has been fixed. 
<br>

## 1.0.9.1004 (2019-03-10)

Cumulative Improvements

#### Added

- Minor UI improvements.
- Import blogs from file.
- Implement CI pipeline support to standardise builds.
<br>

## 1.0.8.68 (2019-03-09)

Scan for non-tumblr photo and video urls

See Details: 

https://github.com/johanneszab/TumblThree/releases/tag/v1.0.8.68

<br>

## 1.0.8.63 (2019-03-09)

Fixes random parsing error for regular Tumblr blog downloads

See Details:

https://github.com/johanneszab/TumblThree/releases/tag/v1.0.8.63

<br>

## 1.0.8.61 (2019-03-09)

Bugfix release

See details:

https://github.com/johanneszab/TumblThree/releases/tag/v1.0.8.61

<br>

---

**The releases in our old repository follow**

---

<br>

## 1.0.8.77 (2021-03-07)

New Home for TumblThree!

**This repository is no longer maintained!**

Please visit our [new github repository at TumblThreeApp](https://github.com/TumblThreeApp/TumblThree) and check out the [releases there](https://github.com/TumblThreeApp/TumblThree/releases)!

<br>

## 1.0.8.76 (2020-08-12)

Fixes Tumblr Searches

Re-implements Tumblr Searches:

* E.g. https://www.tumblr.com/tagged/cars to search for "car" tagged posts, and
* E.g. https://www.tumblr.com/search/cars to search for "car" posts."
<br>

## 1.0.8.75 (2020-07-19)

Use standard .NET TLS version instead of OS TLS version.

This prevents TLS (SChannel 70, EventID 36887) errors and hence unsuccessful connections on Windows versions pre Windows 10. For example, the standard TLS version of Windows 7 is TLS 1.0 which is deprecated.

The settings in this commit sets the TLS version to the standard TLS version of the currently installed .NET Framework (4.7.X) instead of the System/OS default TLS version.

<br>

## 1.0.8.74 (2020-06-21)

Cache StreamWriter instances for text downloading

Stores StreamWriter instances in Dictionary and reuses them for recurring text appends in text post downloading.
This prevents massive seek I/O in large blog downloads, and hence poor (disk) performance.

<br>

## 1.0.8.73 (2020-02-18)

Cumulative Improvements
* Allows to add [tumbex urls](https://www.tumbex.com/) via GUI, text, or clipboard.
* Can open blogs on tumbex.com via the context menu (right mouse click).
* Can download tumblr photos with non-"tumblr_"-prefix.
* You can now choose which tumblr blog scraper you want to use:
  * Tumblr API: The previous default crawler for non-hidden blogs which utilized the Tumblr v1 API, or
  * Tumblr SVC: This service is (was?) used by Tumblr internally for displaying hidden blogs. Using this scraper requires a to be logged in.

  You can change the crawler in the details view of each blogs. Using the SVC crawler implementation might be faster if it's not as much rate limited as accessing the site via the official v1 API.

* Can download higher resolution images if available using the SVC crawler. For this, change all your tumblr blogs to use the SVC crawler via the Details Panel -> Crawler -> "Tumblr SVC". Set the downloadable image size in the Settings (Settings->Connections) to "best". This will download the highest resolution image found.
* It's now possible to set a separate rate limit for the SVC crawler in settings->connections.
* It's now possible to set the default crawler for Tumblr Blogs in the settings in settings->blog. You can choose between "Tumblr API" und "Tumblr SVC". If you do not tick this checkbox, the default automatic detection will add the blog depending on if it's accessible via the Tumblr API. If it's not, then the SVC crawler will be used as it requires to be logged in (see the notes above for more information on which to pick).
* Saves the settings directly after performing changes instead of only on application exit.
* Import blogs from file."
<br>

## 1.0.8.68 (2018-12-11)

Scan for non-tumblr photo and video urls

[Some usage notes/advices for new users.](https://github.com/johanneszab/TumblThree/issues/301)
<br/>
New in this release:
* Fixes an application crash if TumblThree cannot agree to the new Tumblr ToS ([#295](https://github.com/johanneszab/TumblThree/issues/295)([#311](https://github.com/johanneszab/TumblThree/issues/311)([#323](https://github.com/johanneszab/TumblThree/issues/323)).
* TumblThree can now crawl in parallel two different kind of blogs with the same name. For example the "likes" and the regular blog of the same user ([#296](https://github.com/johanneszab/TumblThree/issues/296)).
* Updates German translation (thanks to @fdellwing([#300](https://github.com/johanneszab/TumblThree/issues/300)).
<br/>

* Contains an option to scan everything TumblThree crawls for photos or video urls using regular expressions. It however excludes urls containing *tumblr_*, because otherwise too many duplicates were downloaded in all kinds of resolutions (e.g. tumblr_abc_{128,640,1280}.jpg). This might still add duplicates, but it might also gather some externally hosted photo or videos embedded in (text) posts. I've not tested this, it was just an idea I wanted to add before the 17th December. Maybe it's complete crap. Thus, use it with caution.
* This release contains a lot of code refactoring. If this release doesn't work for you, try the [latest previous release found here](https://github.com/johanneszab/TumblThree/releases/tag/v1.0.8.63). That release should be a lot more mature.

* New in v1.0.8.66:
    * Fixes incorrect handling of photosets in the regular Tumblr blog Crawler ([#328](https://github.com/johanneszab/TumblThree/issues/328)).
    * Downloads Tumblr videos from the v*.tumblr.com hosts ([#285](https://github.com/johanneszab/TumblThree/issues/285)([#320](https://github.com/johanneszab/TumblThree/issues/320)).

* New in v1.0.8.67:
    * Prevents same blog additions when triggering the clipboard monitor rapidly one after another.
    * Downloads Tumblr videos from the v*.tumblr.com hosts, now also in liked-by downloads ([#320](https://github.com/johanneszab/TumblThree/issues/320)).

* New in v1.0.8.68:
    * Prevents application stall if the crawl is canceled immediately after a new crawl started. Previously, in those cases, the crawl button didn't return active after cancel was pressed or a active item remained in the queue, even if the crawler was stopped."
<br>

## 1.0.8.63 (2018-11-07)

Fixes random parsing error for regular Tumblr blog downloads

[Edit: Some usage notes/advices for new users.](https://github.com/johanneszab/TumblThree/issues/301)

* Retries the Tumblr blog api v1 request if the server returns an empty HTTP-200 (OK) answer which resulted in seemingly random parsing errors for regular Tumblr blog downloads ([#280](https://github.com/johanneszab/TumblThree/issues/280)). The maximum retry count is currently set to 3 and can be adjusted by modifying the Settings.json. The corresponding setting is _MaxNumberOfRetries_.
* Adds an option to set the queue information refresh rate (i.e. how many times it updates at most in micro seconds).
* Reverts the default Tumblr photo size from _\\_raw_ to 1280px. The code for handling _\\_raws_ is still there, but the default photo size in the TumblThree settings for new Users is set to 1280px again. This currently saves one failed web request per photo download as TumblThree tries to "guess" the _raw photo url for each photo by just accessing it.
* Checks if there is _\\_files.tumblr_ database for each corresponding _.tumblr_ database at startup.
* Checks if the _.tumblr_ databases and the corresponding _\\_files.tumblr_ databases are valid at startup.

* New in v1.0.8.63:
    * Fixes crawler stall bug that occurred if the blog manager was empty and didn't contain a blog at application startup (i.e. for new users mostly) introduced in the v1.0.8.62 release ([#284](https://github.com/johanneszab/TumblThree/issues/284))."
<br>

## 1.0.8.61 (2018-10-11)

Bugfix release

#### Fixed
* Improves the regex pattern for the detection of inlined tumblr videos within other posts content/bodys ([#271](https://github.com/johanneszab/TumblThree/issues/271) / [#270](https://github.com/johanneszab/TumblThree/issues/270)).
* Uses the content of the trail of each post for the hidden tumblr blog post inline photo and video detection instead of changing fields depending on the posts type ([#274](https://github.com/johanneszab/TumblThree/issues/274)).
* Allows to use () and \ and probably more special characters in the tumblr search and tumblr tag search ([#266](https://github.com/johanneszab/TumblThree/issues/266)).
* Correctly handles SerializationExceptions in the IFiles databases. Previously, the exception wasn't handled at all and would stuck the crawler ([#273](https://github.com/johanneszab/TumblThree/issues/273)).
* Displays a list of blogs that failed to deserialize at startup instead of stopping at the first blog ([#273](https://github.com/johanneszab/TumblThree/issues/273)).
* Continues to load and add all remaining successfully deserialized blogs to the manager ([#273](https://github.com/johanneszab/TumblThree/issues/273)).
* Fixes crawler stall if it was stopped during the online check or maximal post count detection.
* Fixes Tumblr likes download for blogs containing a dash (-) within the name."
<br>

## 1.0.7.63 (2018-10-11)

SVC Release

* If the main release branch (v1.0.8.X) should ever be non functional (because of dead api), this release should still be working since it uses a service required for displaying the website itself. Requires a login to work, but can download all kind of posts without error prone parsing (v1.0.5.X) of the website itself.
* It's also not rate limited which allows multiple instances to run in parallel if the portable mode is used.
* It offers a lot more [meta data](https://www.jzab.de/files/svc_metadata.txt) which could be grabbed if someone implements it.
<br>

## 1.0.5.80 (2018-10-11)

Parsing of the Website

* This release can only download pictures and videos. No text posts. It does however not use the Tumblr api and thus is not rate limited during the url detection/scanning process. For more, see [#33](https://github.com/johanneszab/TumblThree/issues/33).

If unsure, do not download this release.

<br>

## 1.0.8.58 (2018-08-25)

Implements Tumblr login process

* Implements the Tumblr login process and cookie handling in code instead of relying on the Internet Explorer for the Tumblr login process ([#247](https://github.com/johanneszab/TumblThree/issues/247)). If this doesn't work for you, you can safely revert to the [previous release (v1.0.8.51)](https://github.com/johanneszab/TumblThree/releases/tag/v1.0.8.51).

  You'll have to re-authenticate in the settings for downloading likes, posts from the tag search, or hidden tumblr blogs. For this, open the settings window, go to the _connection_ tab and fill in your email address and password used to create your Tumblr account. The email address and the password is used to generate cookies which are now stored in the TumblThree settings folder.

* New in  v1.0.8.53:
    * Implements two-factor authentication for the Tumblr login process ([#247](https://github.com/johanneszab/TumblThree/issues/247)).
* New in  v1.0.8.54:
    * Implements Tumblr logout methods to remove the authentication cookies ([#213](https://github.com/johanneszab/TumblThree/issues/213)).
    * Allows to add tumblr blogs ending with www ([#248](https://github.com/johanneszab/TumblThree/issues/248)).
* New in  v1.0.8.55:
    * Fixes broken release for first time users ([#249](https://github.com/johanneszab/TumblThree/issues/249)). Thanks to @Pdbrantley and @oakgary for the quick notification.
* New in  v1.0.8.57:
    * Includes the Tumblr search and Tumblr tag search in the rate limiter ([#252](https://github.com/johanneszab/TumblThree/issues/252)).
* New in  v1.0.8.58:
    * Adds a context menu item to the blog manager to allow online checking of selected blogs ([#256](https://github.com/johanneszab/TumblThree/issues/256)).
    * Fixes LoliSafe parser (Thanks to @salrana).
<br>

## 1.0.8.51 (2018-06-09)

ToS and GDPR Changes

* Fixes hidden Tumblr blog download problems caused by the new Tumblr ToS ([#240](https://github.com/johanneszab/TumblThree/issues/240)).
<br>

## 1.0.8.50 (2018-05-24)

ToS and GDPR Changes

* Programmatically agrees to new ToS and GDPR ([#229](https://github.com/johanneszab/TumblThree/issues/229)).
* Implements SVC authentication changes. The SVC service is used to display the dash board blogs (i.e. hidden tumblr blogs). Changes in this internal Tumblr api prohibited TumblThrees access ([#229](https://github.com/johanneszab/TumblThree/issues/229)).
* Saves the last post id in successful hidden tumblr downloads ([#225](https://github.com/johanneszab/TumblThree/issues/225)).
* Improves the text parser of the tumblr api and tumblr svc data models. Separated the slug from the url as the data models are inconsistent. Separated the photoset urls from the photo urls. Moved the date information into a separate column ([#227](https://github.com/johanneszab/TumblThree/issues/227)).
* Minor text changes of some user interface elements.
<br>

## 1.0.8.48 (2018-04-18)

Implements Tumblr Datamodel Changes

* Updates the tumblr blog crawler and the hidden tumblr datamodel to reflect tumblr api changes that break blog download of previous TumblThree versions ([#222](https://github.com/johanneszab/TumblThree/issues/222)([#221](https://github.com/johanneszab/TumblThree/issues/221)).
<br>

## 1.0.8.46 (2018-03-23)

Minor improvements and bugfixes

* Properly restores visibility options of hidden columns in the context menu of the blog manager ([#137](https://github.com/johanneszab/TumblThree/issues/137)).
* Removes the proxy options from the settings window as TumblThree now uses the Windows proxy settings.
* Notifies the user if a post couldn't be parsed and was discarded ([#217](https://github.com/johanneszab/TumblThree/issues/217)).
* Fixes proxy settings. They should finally work and allow TumblThree to download content behind the Great Firewall using a proxy set up in the [Windows proxy settings](https://github.com/Emphasia/TumblThree-zh/issues/3).
* Updates Chinese translations (Thanks @Emphasia).
<br>

## 1.0.8.43 (2018-03-03)

Additional video parser

* Allows to download only specific pages of hidden Tumblr blogs and in the tumblr search ([#191](https://github.com/johanneszab/TumblThree/issues/191)).
* Improves the proxy settings. TumblThree now uses the default Windows (Internet Explorer) settings if not overridden within TumblThree ([#204](https://github.com/johanneszab/TumblThree/issues/204)).
* Changes the behavior of the timeout value (Settings->Connection->Timeout). The timeout value now counts file chunks of 4kb instead of the whole file download, thus it should better detect if a download is stalled or a connection dropped without canceling active downloads of larger files (e.g. videos) ([#214](https://github.com/johanneszab/TumblThree/issues/214)).
* Changes default timeout value (for new users) from 600s to 30s.
* Fixes possible download of the same photo but with different resolutions. This happened if the _raw file download was interrupted (the timeout hit), then the same photo was queued for download with the _1280 resolution. If the blog was then subsequently queued again, the _raw file was downloaded next to the _1280 file.
* Fixes reblog/original post detection in the tumblr hidden crawler ([#194](https://github.com/johanneszab/TumblThree/issues/194)).
* Fixes `check blog status during startup`-option ([#208](https://github.com/johanneszab/TumblThree/issues/208)).
* Fixes download of password protected tumblr blogs ([#211](https://github.com/johanneszab/TumblThree/issues/211)).
* Adds Mixtape, Lolisafe, Uguu, Catbox and SafeMoe parser (thanks to @bun-dev([#197](https://github.com/johanneszab/TumblThree/issues/197)).
<br>

## 1.0.8.39 (2018-01-07)

Save text posts and metadata in json format

* Adds a json formatter for saving text posts and metadata of binary posts as json ([#187](https://github.com/johanneszab/TumblThree/issues/187)).
* The queue progress now informs about skipped posts ([#151](https://github.com/johanneszab/TumblThree/issues/151)).
* The context menu in the blog manager now allows to copy the urls of selected blogs to the clipboard.

Note: This release handles the data structures for regular tumblr blogs (i.e. non-hidden tumblr blogs and non-searches) differently and thus might not be as mature for downloading them compared to the [previous release](https://github.com/johanneszab/TumblThree/releases/tag/v1.0.8.36). If you notice anything odd ([#187](https://github.com/johanneszab/TumblThree/issues/187)), you might want to give that a try.

<br>

## 1.0.8.36 (2017-12-31)

Minor enhancements

* Fixes a bug that released the video connection semaphore too often. That means the slider in the settings for limiting the video downloads didn't work at all. It should properly limit the connections to the vt.tumblr.com host and prevent incomplete video downloads now.
* Includes a rewrite of the blog detection during blog addition. It should reduce latency if you mass add blogs by copying urls into the clipboard (ctrl-c). Offline blogs aren't added anymore.
* Notifies the user when a connection timeout has occurred. The message states whether the timeout has occurred during downloading or crawling. If it happened during crawling, you might want to re-queue the blog at some point to grab missing posts. A connection timeout should only happen if your connection is wonky. You can decrease/increase the timeout in the settings (settings->connection).
* You can now specify in the _Details_ panel for each blog where its files should be downloaded. If the text box control is empty, the files are downloaded as in previous releases in the folder specified in the global download location (settings->general), plus the blogs name.
* Imgur.com linked albums in tumblr posts are now entirely downloaded if enabled (details panel->external->download imgur). Previously, only directly linked images were detected.
* Adds an option to load all blog databases into memory and compare each to-download binary file to all databases across TumblThree before downloading. If the file has already been downloaded in any blog before, the file is skipped and will not be counted as downloaded. You can enable this in the settings (settings->global) ([#179](https://github.com/johanneszab/TumblThree/issues/179)([#151](https://github.com/johanneszab/TumblThree/issues/151)).
* Allows to add hidden tumblr blogs using the dashboard url (i.e. [https://www.tumblr.com/dashboard/blog/__blogtobackup__](https://www.tumblr.com/dashboard/blog/blogtobackup)).
* Allows to add all blog types without the protocol suffix (i.e. wallpaperfx.tumblr.com, www.tumblr.com/search/cars).
* Adds an option to enable a confirmation dialog before removing blogs ([#186](https://github.com/johanneszab/TumblThree/issues/186)([#130](https://github.com/johanneszab/TumblThree/issues/130)([#98](https://github.com/johanneszab/TumblThree/issues/98)). It's off by default.
<br>

## 1.0.8.32 (2017-11-18)

Download of Imgur, Gfycat, Webmshare hosted files

* Adds support for downloading Imgur.com, Gfycat.com and Webmshare.com linked files in tumblr posts.
* Improves downloading of tumblr liked/by photos and videos ([#171](https://github.com/johanneszab/TumblThree/issues/171)([#78](https://github.com/johanneszab/TumblThree/issues/78)).
* Allows to download tumblr liked/by photos and videos within a defined time span.
* Fixed application crashes in the tumblr search/tumblr tag search/tumblr liked by and tumblr hidden blog crawler if the connection had to be terminated because it was over the defined timeout in the settings panel. That most likely should only happen for bad internet connections. The code still misses user notification about those events ([#174](https://github.com/johanneszab/TumblThree/issues/174)).
<br>

## 1.0.8.27 (2017-10-13)

More translations

* Fixes crawler stop in hidden tumblr blog downloads if only original content should be downloaded (thanks to anon for pointing this out).
* Adds options to set the default blog settings for the _download from_ time, _download to_ time and _tags_ in the settings menu.
* Adds more broken google translate translations. Since they are larger than the application itself, all the translations are now in a separate .zip file.
  To use the translations, extract both files into the same _TumblThree_ folder so that the language folders are sub-folders of the _TumblThree.exe_ file. You can orient yourself in the already included _en_ folder in the Application.zip file itself. All unwanted language folders can safely be removed.

  Included are now ar, de, el, en, es, fa, fi, fr, he, hi, it, ja, ko, no, pa, pl, pt, ru, th, tr, vi and zh translations.

* New in  v1.0.8.27:
    * Changes the default _raw photo host from media.tumblr.com to data.tumblr.com ([#158](https://github.com/johanneszab/TumblThree/issues/158)). Special thanks to all the people providing me with information and performing various tests!  __Note:__ You'll have to manually remove your settings.json to fix the _raw download host, or change the _TumbrHosts_ field string to _data.tumblr.com_. The file is located in _C:\\Users\\YOURUSERNAME\\AppData\\Local\\TumblThree\\Settings\\\\_. For more information about this field, you can check [my post on my website](https://www.jzab.de/comment/3938#comment-3938).
    * Makes the TumblrHost property setable. This allows to update/change the _raw host in the settings.json.
    * __Note__: Tumblr seems to host its _raw images (now) on amazon S3 but forgot to update the hosts in their ssl cert, thus all connections fail with _NET::ERR_CERT_COMMON_NAME_INVALID_. We now temporarily __trust all certs__ until it's fixed.
    * Updates zh, ru, fr, de translations. 
<br>

## 1.0.8.24 (2017-09-23)

Download of password protected blogs

* Can download non-hidden, password protected blogs.
* UI changes:
  * Added a password textbox in the details tab for supplying a password if its necessary for accessing the blog.
  * Moved the tags column out of the blog manager into the details tab.
  * Removed the now redundant 'Check directory for files'-checkbox since the downloader is capable of resuming files, it checks for the files existence anyways.
  * Added a 'blog type' column in the blog manager denoting which downloader is used.
* Updated Chinese translation (thanks to @Emphasia).
* New in  v1.0.8.22:
    * Fixes newly introduced (v1.0.8.21) crash if a tumblr search with more than one keyword/tag was added ([#139](https://github.com/johanneszab/TumblThree/issues/139)).
    * Updates English text in the user interface and tool tips. More user interface cleanup will follow ..
* New in  v1.0.8.23:
    * Downloads inlined photos in photo posts and inlined videos in video posts. I think every inlined photo/video should be covered now. I've excluded scanning for inlined photos in, and only in, photo posts previously to not scan the same photo twice. I also wasn't aware that you can add a photo to a photo post. Same applies to video posts (thanks to anon for pointing this out).
    * Add a separate maximum video connection value in the settings window. Someone has to test if this helps downloading mixed video & photo blogs with mostly video content. If the connection value for videos is set too high, TumblThree might not completely download them, but also not count them as downloaded, since the tumblr video host (vt.tumblr.com) closes all connections if there are too many open for a too long time. But still, one has to re-queue/download them to eventually finish all downloads. Please see [#141](https://github.com/johanneszab/TumblThree/issues/141) for more.
* New in  v1.0.8.24:
    * Somewhat "fixes" the timeout. Thus, if you have a wonky connection that frequently gets interrupted, TumblThree shouldn't stall anymore. The timeout value now counts for the whole connection time regardless of it's state. E.g. if you won't finish downloading a large file (video) within 120 seconds (default) increase the value or the file is truncated. If the release has any side effects since I've had to modify the core webrequest/downloader/crawler code for this, please try the v1.0.8.22 ([#116](https://github.com/johanneszab/TumblThree/issues/116)).

_Note:_ Before upgrading, remove the _ColumnSettings_ from the _settings.json_ in _C:\\Users\\YOURUSERNAME\\AppData\\Local\\TumblThree\\Settings\\\\_ or delete the file entirely, otherwise you'll get a _Could not restore ui settings_-error. If you remove the _settings.json_ file, you'll have to reset all your settings afterwards."

<br>

## 1.0.8.20 (2017-09-03)

Tumblr search downloader

* A downloader for downloading photos and videos from the tumblr tag search (e.g. http://www.tumblr.com/tagged/my+keywords) (login required). The keywords should be separated by plus signs (+). See [#97](https://github.com/johanneszab/TumblThree/issues/97) for more.
* A downloader for downloading photos and videos from the tumblr search (e.g. http://www.tumblr.com/search/my+keywords). The keywords should be separated by plus signs (+). It only returns around 50-150 posts. See [#97](https://github.com/johanneszab/TumblThree/issues/97) for more.
* Allows to download blog posts in a defined time span.
* Customized detail views for each downloaders capability depending on the selection in the manager.
* Code refactoring.
* Bugfixes.

_Note:_ After upgrading from previous releases, delete your settings (or just the Queuelist.json) in _C:\\Users\\YOURUSERNAME\\AppData\\Local\\TumblThree\\Settings\\\\_ if you get the _error 1: The queue list could not be loaded_ message.

<br>

## 1.0.8.13 (2017-08-22)

Bugfixes & Code Refactoring

* Removes user interface lag during blog addition.
* Stop now also stops (and saves the active databases) if the network connection was/is disrupted.
* Uses .NET Framework 4.6 now as it should be available for all supported windows versions (Windows Vista and above). If it doesn't work anymore let me know. I don't use any new features of this version in the code so we could still stick to .NET version 4.5, but they improved the memory handling (garbage collection) next to some other things. Maybe it's worth it.
* Code Refactoring.
* Updates Chinese translation (thanks to @Emphasia).
* Updates French translation (thanks to @willemijns).
* New in  v1.0.8.9:
    * Updates Chinese translation.
    * Fixes parsing of meta data in hidden blogs.
    * Fixes bug introduced in v1.0.8.8 which prevented downloading "liked/by" posts.
* New in  v1.0.8.10:
    * Improved the selection handling in the details panel. If multiple blogs are selected, old values are now kept if they are the same for all blogs and changes are immediately reflected.
* New in  v1.0.8.11:
    * Adds audio file download support for tumblr and hidden tumblr blogs."
<br>

## 1.0.8.2 (2017-07-15)

Allows to download hidden blogs

* Allows to download hidden tumblr blogs (that require a login to view/dashboard blogs). For this you have to login to tumblr.com using either the Internet Explorer or you can do it within TumblThree under _Settings->Authenticate_. The same cookie will be used. For non-hidden blogs however, you don't have to login. There are two separate downloader, one for each blog type.
* Finally a proper \\_raw (original / high resolution) tumblr image file handling. The file dimension size from the crawler is now always tested as the last fallback if no \\_raw file was found. The defaults are sane now without introducing to much latency or dropping/stalling downloads if the _raw file was not found.
* Fixed duplicate downloading due to \\_raw file introduction. If the same url was detected multiple times (e.g. double post in the blog) by the crawler and ended up in close proximity in the downloader queue, it might have happened that the same image was downloaded twice by different processes but in a different file size. 

For more advanced users:
* The _settings.json_ in _C:\\Users\\Username\\AppData\\Local\\TumblThree\\Settings\\\\_ contains a list of hosts which can be modified named TumblrHosts. These hosts are tested for \\_raw image files in order, now containing only the _media.tumblr.com_ host.
<br>

## 1.0.6.8 (2017-07-11)

Bugfixes & Performance Improvements

* Improved cpu usage: The cpu usage should stay below a quarter of a core now. Previously the scanning hogged a lot of cpu cycles to prevent adding duplicates to the download queue which scaled inversely with the blog size.
* Improved memory usage: The file downloader was not properly uncoupled from the cancellation mechanism, resulting in an increasing memory usage with each download. The collected blog statistics (number of posts, kind of posts, etc.) are now also early removed from the memory and not held in memory until the complete crawl is finished.
* Correct cancellation handling (stopping of the crawler tasks).
* _Raw file support:
  * if no __raw_ file is available, the downloader tries the __1280_ file.
  * Fixes dropping download speeds after a while introduced with the probing for _raw files and an unhandled failing of those ([#101](https://github.com/johanneszab/TumblThree/issues/101)).
* More stability improvements.

This release is the continuation of the v1.0.4 branch. If unsure, download this release. If you use the version v1.0.4.31 or anything later, you should really update to this one.

<br>

## 1.0.4.59 (2017-06-21)

Download high resolution images.

* Downloads high resolution (_raw .jpg | .png | .gif) images. Since this size isn't offered by the api, all image urls are now forcefully renamed to your settings (Settings->Imagesize).

__Note:__ This might result in a re-download of the same image again, but with a different filename.

<br>

## 1.0.4.57 (2017-06-18)

Download specific pages

* Sets the _Date modified_ date in the Explorer to the posts time. It allows to view the blog chronologically by sorting by date. E.g. if a picture was posted on June 04, 2013, the date of that picture will be June 04, 2013.
* Allows to download single or ranges of blog pages. Valid formats are comma separated values or ranges. E.g: 
  * __1,2,3__ downloads the pages 1 and 2 and 3. 
  * __1-10__ downloads the pages 1 till 10.
  * If entered nothing the whole blog is being downloaded. 
  * You can set the _posts per page_ between 1 to 50 used for crawling. E.g. settings it to 50 will scan 50 posts per page. If nothing is set, 50 posts will be set.
* Clicking the preview opens the preview in a full screen window.
* An option to export all blog urls as a text file in the settings (settings -> general -> Export Blogs). One url per row. This allows a quick transfer of all blogs to a different TumblThree instance by simply opening the generated .txt file, select all blogs and copy them into the clipboard (i.e. ctrl-a, ctrl-c).
* Updates Russian translation (thanks @blackgur).
* Updates German translation.
* Applies all settings changes immediately if possible without application restart. Changing the download location during an active download still requires a manual restart.
<br>

## 1.0.4.48 (2017-05-20)

Skip reblogged posts.

* Adds an option to skip reblogged posts and download only original content from the author.
* Improves the download of inlined photos and videos in text posts (e.g. a picture in a answer posts).
* Other minor bugfixes (see the last six commits).

Note: You have to set _Download reblogged posts_ for each old dataset. Simply select all blogs (ctrl-a) and mark the checkbox in the _Details_ view.

<br>

## 1.0.4.47 (2017-05-16)

Code refactoring.

#### Changed
I've changed quite a lot internally. You can check out the last ~75 commits. Most of them were code refactoring and code enhancements. Should be mostly bugfree now.

#### Added
* Resumes incomplete downloads.
* Fixes incomplete video download.
* Downloader now stops immediately when stopping as downloads are resumable. 
* Saves application settings now as json instead of xml. So you have to reset everything in the settings.
* The preview doesn't lag anymore and does not stall the application.
* It's now possible to drag&drop blogs from the manager (left) to the queue.
* An option to check the directory for already download files besides the internal database ([#44](https://github.com/johanneszab/TumblThree/issues/44)).
* An option to download an url list instead of the actual binary files ([#42](https://github.com/johanneszab/TumblThree/issues/42)).
* Fixes application crash if a drag&drop was initiated during a cell edit (e.g. tags cell) ([#66](https://github.com/johanneszab/TumblThree/issues/66)).
* An application update checker.
* Downloads liked photos and videos, see [#74](https://github.com/johanneszab/TumblThree/issues/74) for more. For downloading those, you have to do some steps:
  1. Go to _Settings_, click the _Authenticate_ button. Logon to tumblr using an account. The window/browser should automatically close after the login indicating a successful authentication. TumblThree will use the Internet Explorer cookies for authentication.
  2. Add the blog url including the _liked/by_ string in the url (e.g. https://www.tumblr.com/liked/by/wallpaperfx/).
* Allows to change the visibility of the columns in the manager. There is a bug right now where you have to remove and re-add a column to display previously removed columns again. 
* Adds a portable mode which stores the application settings next to the executable instead in the AppData folder.
* Fixes bandwidth throttling. Also allows to completely bypass it by setting the value to 0 in the settings.
* Allows to set proxy credentials (ProxyUsername, ProxyPassword) in _plaintext_ in the settings file. Not tested.

#### Fixed
* Fixes UI stall if many blogs were added using the ClipboardManager ([#18](https://github.com/johanneszab/TumblThree/issues/18)).
* Fixes the autodownload function. Previously the stored value in the Settings.xml was used, not the one currently set ([#63](https://github.com/johanneszab/TumblThree/issues/63)).

Bufixes since the first code refactoring (v1.0.4.31) release include:
* Fixes downloading of tagged files.
* Fixes application crash if a blog is added that is empty ([#40](https://github.com/johanneszab/TumblThree/issues/40)).
* Fixes possible downloader stall ([#75](https://github.com/johanneszab/TumblThree/issues/75)).
* Improves the photo and video detection in the tumblr likedby downloader ([#77](https://github.com/johanneszab/TumblThree/issues/77)).

__Note:__ If you have old binary data files (.tumblr) without the separated file list (_files.tumblr) you need to convert the big files into two smaller ones using the [v1.0.4.31 release](https://github.com/johanneszab/TumblThree/releases/tag/v1.0.4.31). After that you can use any of the newer releases.

<br>

## 1.0.4.31 (2017-03-16)

The Tumblr api is now rate limited.

**Backup** your Index folder in the download location before running this version. It will permanently modify your blog index files (*.tumblr) upon the first run. They contain the already downloaded file information and might end up broken after the upgrade. 
* Saves blog databases as .json files (plain text) instead of a binary format. Allows modification in your text editor of choice.
* The url list is now a separated file (_files.tumblr, also saved as json) and loaded on demand and is not permanently held in memory to reduce memory usage.
* Stores only the filename of tumblr photo, video and audio posts, instead of the whole url. This lowers memory consumption as a large part of the url is not file but host specific. The whole url address was saved to prevent reloading of the same file, but since the host server changes, the filename should be sufficient for this task.
* The picture/video preview lags a bit in the beginning and might display nothing for several seconds but does not freeze the whole application anymore.
* Downloads inline images of all post types ([#24](https://github.com/johanneszab/TumblThree/issues/24)).
* The picture preview now displays animated .gifs ([#38](https://github.com/johanneszab/TumblThree/issues/38)).

**Rate limited Tumblr api:**
The initial download process where all the image, video and audio urls are being searched for has to be slowed down since mid-February of 2017. The servers now only accept a defined number of connections per time interval. If too many connections are opened the servers don't respond anymore and just close the connection with a 429 respond -- Limit exceeded (see [#26](https://github.com/johanneszab/TumblThree/issues/26) for more).

Therefore, this pre-release addresses this new issue by:
* Adding a rate limiter in the settings. The _Number of connections_ is per _time_ in seconds and might be increased. I've not tested these two values thoroughly, but they work without hitting the limit. [Different solutions](https://github.com/johanneszab/TumblThree/issues/26#issuecomment-282575498) as mention in [#26](https://github.com/johanneszab/TumblThree/issues/26) are faster (e.g. crawl in small batches and start the download immediately) but require more work to properly implement them. Only the initial evaluation period for grabbing the urls and meta information is slowed down. The picture, video and audio download is not impacted.
* It now shows an error if the api limit was reached. You should lower the limit for the api connections in the settings and re-crawl the specific blog, otherwise not all posts will be downloaded.
* Brings back some speed by simultaneously accessing the api and immediately downloading the first grabbed image, video and audio urls. So it does not wait for the _"evaluating xxx of xxx post"_ to finish before starting to download.
* If a blog was successfully downloaded, the newest post id is saved. Upon the next download, only newer posts will be evaluated using the tumblr api, thus finishing the blog more quickly. A full rescan can be forced in the details view."
