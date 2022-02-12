﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Xml.Linq;

using TumblThree.Applications.DataModels;
using TumblThree.Applications.DataModels.TumblrApiJson;
using TumblThree.Applications.DataModels.CrawlerData;
using TumblThree.Applications.Downloader;
using TumblThree.Applications.Parser;
using TumblThree.Applications.Properties;
using TumblThree.Applications.Services;
using TumblThree.Domain;
using TumblThree.Domain.Models;
using TumblThree.Domain.Models.Blogs;
using TumblThree.Domain.Models.Files;
using TumblThree.Applications.DataModels.Twitter.TimelineTweets;

namespace TumblThree.Applications.Crawler
{
    [Export(typeof(ICrawlerFactory))]
    public class CrawlerFactory : ICrawlerFactory
    {
        private readonly ICrawlerService crawlerService;
        private readonly IManagerService managerService;
        private readonly IShellService shellService;
        private readonly ISharedCookieService cookieService;
        private readonly AppSettings settings;

        [ImportingConstructor]
        internal CrawlerFactory(ICrawlerService crawlerService, IManagerService managerService, ShellService shellService,
            ISharedCookieService cookieService)
        {
            this.crawlerService = crawlerService;
            this.managerService = managerService;
            this.shellService = shellService;
            this.cookieService = cookieService;
            settings = shellService.Settings;
        }

        [ImportMany(typeof(ICrawler))]
        private IEnumerable<Lazy<ICrawler, ICrawlerData>> DownloaderFactoryLazy { get; set; }

        public ICrawler GetCrawler(IBlog blog)
        {
            Lazy<ICrawler, ICrawlerData> downloader =
                DownloaderFactoryLazy.FirstOrDefault(list => list.Metadata.BlogType == blog.GetType());

            if (downloader != null)
            {
                return downloader.Value;
            }

            throw new ArgumentException("Website is not supported!", nameof(blog));
        }

        public ICrawler GetCrawler(IBlog blog, IProgress<DownloadProgress> progress, PauseToken pt, CancellationToken ct)
        {
            IPostQueue<AbstractPost> postQueue = GetProducerConsumerCollection();
            IFiles files = LoadFiles(blog);
            IWebRequestFactory webRequestFactory = GetWebRequestFactory();
            IImgurParser imgurParser = GetImgurParser(webRequestFactory, ct);
            IGfycatParser gfycatParser = GetGfycatParser(webRequestFactory, ct);
            switch (blog.BlogType)
            {
                case BlogTypes.tumblr:
                    IPostQueue<CrawlerData<Post>> jsonApiQueue = GetJsonQueue<Post>();
                    return new TumblrBlogCrawler(shellService, crawlerService, webRequestFactory, cookieService,
                        GetTumblrDownloader(progress, blog, files, postQueue, pt, ct), GetJsonDownloader(jsonApiQueue, blog, pt, ct),
                        GetTumblrApiJsonToTextParser(blog), GetTumblrParser(), imgurParser, gfycatParser, GetWebmshareParser(),
                        GetMixtapeParser(), GetUguuParser(), GetSafeMoeParser(), GetLoliSafeParser(), GetCatBoxParser(), postQueue,
                        jsonApiQueue, blog, progress, pt, ct);
                case BlogTypes.tmblrpriv:
                    IPostQueue<CrawlerData<DataModels.TumblrSvcJson.Post>> jsonSvcQueue =
                        GetJsonQueue<DataModels.TumblrSvcJson.Post>();
                    return new TumblrHiddenCrawler(shellService, crawlerService, webRequestFactory,
                        cookieService, GetTumblrDownloader(progress, blog, files, postQueue, pt, ct),
                        GetJsonDownloader(jsonSvcQueue, blog, pt, ct), GetTumblrSvcJsonToTextParser(blog), GetTumblrParser(),
                        imgurParser, gfycatParser, GetWebmshareParser(), GetMixtapeParser(), GetUguuParser(), GetSafeMoeParser(),
                        GetLoliSafeParser(), GetCatBoxParser(), postQueue, jsonSvcQueue, blog, progress, pt, ct);
                case BlogTypes.tlb:
                    return new TumblrLikedByCrawler(shellService, crawlerService, webRequestFactory,
                        cookieService, GetTumblrDownloader(progress, blog, files, postQueue, pt, ct), GetTumblrParser(),
                        imgurParser, gfycatParser, GetWebmshareParser(), GetMixtapeParser(), GetUguuParser(),
                        GetSafeMoeParser(), GetLoliSafeParser(), GetCatBoxParser(), postQueue, blog, progress, pt, ct);
                case BlogTypes.tumblrsearch:
                    IPostQueue<CrawlerData<string>> jsonQueue = GetJsonQueue<string>();
                    return new TumblrSearchCrawler(shellService, crawlerService, webRequestFactory,
                        cookieService, GetTumblrDownloader(progress, blog, files, postQueue, pt, ct), GetJsonDownloader(jsonQueue, blog, pt, ct),
                        GetTumblrApiJsonToTextParser(blog), GetTumblrParser(), imgurParser, gfycatParser, GetWebmshareParser(),
                        GetMixtapeParser(), GetUguuParser(), GetSafeMoeParser(), GetLoliSafeParser(), GetCatBoxParser(), postQueue,
                        jsonQueue, blog, progress, pt, ct);
                case BlogTypes.tumblrtagsearch:
                    IPostQueue<CrawlerData<DataModels.TumblrTaggedSearchJson.Datum>> jsonTagSearchQueue =
                        GetJsonQueue<DataModels.TumblrTaggedSearchJson.Datum>();
                    return new TumblrTagSearchCrawler(shellService, crawlerService, webRequestFactory,
                        cookieService, GetTumblrDownloader(progress, blog, files, postQueue, pt, ct),
                        GetJsonDownloader(jsonTagSearchQueue, blog, pt, ct), GetTumblrParser(),
                        imgurParser, gfycatParser, GetWebmshareParser(), GetMixtapeParser(), GetUguuParser(),
                        GetSafeMoeParser(), GetLoliSafeParser(), GetCatBoxParser(), postQueue, jsonTagSearchQueue, blog, progress, pt, ct);
                case BlogTypes.twitter:
                    IPostQueue<CrawlerData<Tweet>> jsonTwitterQueue = GetJsonQueue<Tweet>();
                    return new TwitterCrawler(shellService, crawlerService, progress, webRequestFactory,
                        cookieService, postQueue, jsonTwitterQueue, blog, GetTwitterDownloader(progress, blog, files, postQueue, pt, ct),
                        GetJsonDownloader(jsonTwitterQueue, blog, pt, ct), pt, ct);
                default:
                    throw new ArgumentException("Website is not supported!", nameof(blog));
            }
        }

        private IFiles LoadFiles(IBlog blog)
        {
            if (settings.LoadAllDatabases)
            {
                var files = managerService.Databases.FirstOrDefault(file => file.Name.Equals(blog.Name) && file.BlogType.Equals(blog.OriginalBlogType));
                if (files == null)
                {
                    var s = string.Format("{0} ({1})", blog.Name, blog.BlogType);
                    Logger.Error(Resources.CouldNotLoadLibrary, s);
                    shellService.ShowError(new KeyNotFoundException(), Resources.CouldNotLoadLibrary, s);
                    throw new KeyNotFoundException(s);
                }
                return files;
            }

            return Files.Load(blog.ChildId);
        }

        private IWebRequestFactory GetWebRequestFactory()
        {
            return new WebRequestFactory(shellService, cookieService, settings);
        }

        private ITumblrParser GetTumblrParser()
        {
            return new TumblrParser();
        }

        private IImgurParser GetImgurParser(IWebRequestFactory webRequestFactory, CancellationToken ct)
        {
            return new ImgurParser(settings, webRequestFactory, ct);
        }

        private IGfycatParser GetGfycatParser(IWebRequestFactory webRequestFactory, CancellationToken ct)
        {
            return new GfycatParser(settings, webRequestFactory, ct);
        }

        private IWebmshareParser GetWebmshareParser()
        {
            return new WebmshareParser();
        }

        private IMixtapeParser GetMixtapeParser()
        {
            return new MixtapeParser();
        }

        private IUguuParser GetUguuParser()
        {
            return new UguuParser();
        }

        private ISafeMoeParser GetSafeMoeParser()
        {
            return new SafeMoeParser();
        }

        private ILoliSafeParser GetLoliSafeParser()
        {
            return new LoliSafeParser();
        }

        private ICatBoxParser GetCatBoxParser()
        {
            return new CatBoxParser();
        }

        private FileDownloader GetFileDownloader(CancellationToken ct)
        {
            return new FileDownloader(settings, ct, GetWebRequestFactory(), cookieService);
        }

        private static IBlogService GetBlogService(IBlog blog, IFiles files)
        {
            return new BlogService(blog, files);
        }

        private TwitterDownloader GetTwitterDownloader(IProgress<DownloadProgress> progress, IBlog blog, IFiles files,
            IPostQueue<AbstractPost> postQueue, PauseToken pt, CancellationToken ct)
        {
            return new TwitterDownloader(shellService, managerService, ct, pt, progress, postQueue, GetFileDownloader(ct),
                crawlerService, blog, files);
        }

        private TumblrDownloader GetTumblrDownloader(IProgress<DownloadProgress> progress, IBlog blog, IFiles files,
            IPostQueue<AbstractPost> postQueue, PauseToken pt, CancellationToken ct)
        {
            return new TumblrDownloader(shellService, managerService, pt, progress, postQueue, GetFileDownloader(ct),
                crawlerService, blog, files, ct);
        }

        private TumblrXmlDownloader GetTumblrXmlDownloader(IPostQueue<CrawlerData<XDocument>> xmlQueue, IBlog blog,
            PauseToken pt, CancellationToken ct)
        {
            return new TumblrXmlDownloader(shellService, pt, xmlQueue, crawlerService, blog, ct);
        }

        private JsonDownloader<T> GetJsonDownloader<T>(IPostQueue<CrawlerData<T>> jsonQueue, IBlog blog,
            PauseToken pt, CancellationToken ct)
        {
            return new JsonDownloader<T>(shellService, pt, jsonQueue, crawlerService, blog, ct);
        }

        private IPostQueue<AbstractPost> GetProducerConsumerCollection()
        {
            return new PostQueue<AbstractPost>();
        }

        private ITumblrApiXmlToTextParser GetTumblrApiXmlToTextParser()
        {
            return new TumblrApiXmlToTextParser();
        }

        private ITumblrToTextParser<Post> GetTumblrApiJsonToTextParser(IBlog blog)
        {
            switch (blog.MetadataFormat)
            {
                case MetadataType.Text:
                    return new TumblrApiJsonToTextParser<Post>();
                case MetadataType.Json:
                    return new TumblrApiJsonToJsonParser<Post>();
                default:
                    throw new ArgumentException("Website is not supported!", nameof(blog));
            }
        }

        private ITumblrToTextParser<DataModels.TumblrSvcJson.Post> GetTumblrSvcJsonToTextParser(IBlog blog)
        {
            switch (blog.MetadataFormat)
            {
                case MetadataType.Text:
                    return new TumblrSvcJsonToTextParser<DataModels.TumblrSvcJson.Post>();
                case MetadataType.Json:
                    return new TumblrSvcJsonToJsonParser<DataModels.TumblrSvcJson.Post>();
                default:
                    throw new ArgumentException("Website is not supported!", nameof(blog));
            }
        }

        private IPostQueue<CrawlerData<XDocument>> GetApiXmlQueue()
        {
            return new PostQueue<CrawlerData<XDocument>>();
        }

        private IPostQueue<CrawlerData<T>> GetJsonQueue<T>()
        {
            return new PostQueue<CrawlerData<T>>();
        }
    }
}
