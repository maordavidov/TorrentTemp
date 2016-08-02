using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentDownloader
{
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography.X509Certificates;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks.Dataflow;

    using Model;

    using MongoDB.Bson;
    using MongoDB.Driver;

    using MongoDBProxy;

    public class FileListDownloader
    {
        private readonly HttpClient _httpClient;

        private readonly TorrentCollectionProxy _torrentCollectionProxy = new TorrentCollectionProxy();
        private readonly SubtitleCollectionProxy _subtitleCollectionProxy = new SubtitleCollectionProxy();

        public FileListDownloader()
        {
            var cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler()
            {
                CookieContainer = cookieContainer
            };

            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://filelist.ro")
            };

        }

        private async Task<bool> TryLoginAsync()
        {
            Dictionary<string, string> body = new Dictionary<string, string>
            {
                ["username"] = "maord",
                ["password"] = "wQDPPYufJJ"
            };

            HttpResponseMessage httpResponseMessage = await _httpClient.PostAsync("takelogin.php", new FormUrlEncodedContent(body));

            Console.WriteLine(httpResponseMessage.RequestMessage.RequestUri.AbsolutePath);
            //if (httpResponseMessage.RequestMessage.RequestUri.AbsolutePath != "/my.php")
            //{
            //    throw new UnauthorizedAccessException("failed to connect as 'maord' to FileList");
            //}

            return true;
        }

        public async Task DownloadMetadataAsync(VideoInfo videoInfo)
        {
            bool isLoginSucceed = await TryLoginAsync();
            string searchTerm = videoInfo.IMDBInfo.Hash;
            string queryStringFormat = $"browse.php?cat=21&sort=5&searchin=0&search={searchTerm}&page={{0}}";

            for (short pageNubmer = 0; pageNubmer < short.MaxValue; pageNubmer++)
            {
                string query = String.Format(queryStringFormat, pageNubmer);
                string responseHtml = await _httpClient.GetStringAsync(query);
                if (responseHtml.Contains("Nu s-a gasit nimic!"))
                    break;

                IEnumerable<Match> matches = Regex.Matches(responseHtml, "torrentrow(?<torrentRow>.+?)clearfix").Cast<Match>();
                IEnumerable<string> torrentRows = matches.Select(match => match.Groups["torrentRow"]?.Value).Where(v => v != null);


                IEnumerable<TorrentInfo> torrentInfos = torrentRows.Select(torrentRow =>
                        {
                            Match titleMatch = Regex.Match(torrentRow, "title='(?<title>.+?)'>");
                            Match downloadLinkMatch = Regex.Match(torrentRow, "<a\\shref=\"(?<downloadLink>download.php.+?)\"");

                            if (titleMatch.Success && downloadLinkMatch.Success)
                            {
                                string title = titleMatch.Groups["title"].Value.ToLower().Replace(".rosubbed", String.Empty);
                                string downloadLink = downloadLinkMatch.Groups["downloadLink"].Value;
                                string fullUrl = $"{_httpClient.BaseAddress.AbsoluteUri}{downloadLink}";
                                Console.WriteLine(fullUrl);
                                return new TorrentInfo(torrentName: title, imdbHash: videoInfo.IMDBInfo.Hash, downloadLink: fullUrl);
                            }

                            return null;
                        });

                await _torrentCollectionProxy.InsertManyAsync(torrentInfos);
            }
        }

        public IEnumerable<DownloadBundle> DownloaTorrent(VideoInfo videoInfo)
        {
            IEnumerable<DownloadBundle> fullSeasonsBundles = GenerateFullSeasonsBundles(videoInfo).ToList();

            IEnumerable<DownloadBundle> allBundles = GenerateAllTvShowBundles(fullSeasonsBundles, videoInfo);

            return allBundles;
        }

        private IEnumerable<DownloadBundle> GenerateAllTvShowBundles(IEnumerable<DownloadBundle> fullSeasonsBundles, VideoInfo videoInfo)
        {
            IMongoCollection<TorrentInfo> torrentsCollection = _torrentCollectionProxy.GetMongoCollection();
            IMongoCollection<SubtitleInfo> subtitlesCollection = _subtitleCollectionProxy.GetMongoCollection();

            FilterDefinition<TorrentInfo> filterByHash = Builders<TorrentInfo>.Filter.Eq(doc => doc.ImdbHash, videoInfo.IMDBInfo.Hash);

            foreach (DownloadBundle fullSeasonsBundle in fullSeasonsBundles)
            {
                int seasonNumber = fullSeasonsBundle.Season;
                int eposodesNumber = fullSeasonsBundle.VideoInfo.GetEpisodeCountBySeason(seasonNumber);
                for (int episodeNumber = 1; episodeNumber <= eposodesNumber; episodeNumber++)
                {
                    FilterDefinition<TorrentInfo> filter = filterByHash & Builders<TorrentInfo>.Filter.Regex(doc => doc.TorrentName, new BsonRegularExpression($"s0{seasonNumber}"));
                    IFindFluent<TorrentInfo, TorrentInfo> torrents = torrentsCollection.Find(filter);
                }
            }




            return null;
        }

        private IEnumerable<DownloadBundle> GenerateFullSeasonsBundles(VideoInfo videoInfo)
        {
            IMongoCollection<TorrentInfo> torrentsCollection = _torrentCollectionProxy.GetMongoCollection();
            IMongoCollection<SubtitleInfo> subtitlesCollection = _subtitleCollectionProxy.GetMongoCollection();

            FilterDefinition<TorrentInfo> torrentsFilter = Builders<TorrentInfo>.Filter.Eq(doc => doc.ImdbHash, videoInfo.IMDBInfo.Hash) &
                                                           Builders<TorrentInfo>.Filter.Regex(doc => doc.TorrentName, new BsonRegularExpression(@"/S\d\d[\.]+/i"));

            IFindFluent<TorrentInfo, TorrentInfo> torrentsResults = torrentsCollection.Find(torrentsFilter);
            foreach (TorrentInfo torrentInfo in torrentsResults.ToEnumerable())
            {
                string seasonNumberAsString = Regex.Match(torrentInfo.TorrentName, @"s(?<season>\d\d)").Groups["season"].Value;
                string subtitlesRegex = Regex.Replace(torrentInfo.TorrentName, @"s\d\d", $@"s{seasonNumberAsString}e\d\d");
                FilterDefinition<SubtitleInfo> subtitlesFilter = Builders<SubtitleInfo>.Filter.Eq(doc => doc.ImdbHash, videoInfo.IMDBInfo.Hash) &
                                                                 Builders<SubtitleInfo>.Filter.Regex(doc => doc.Name, new BsonRegularExpression($"/{subtitlesRegex}/i"));
                List<SubtitleInfo> subtitles = subtitlesCollection.Find(subtitlesFilter).ToList();



                int episodesCount = subtitles.Select(subtitle => Regex.Match(subtitle.Name, @"e(?<episode>\d\d)?"))
                                              .Select(s => s.Groups["episode"].Value)
                                              .Select(int.Parse)
                                              .Distinct().Count();

                int seasonNumber = Int32.Parse(seasonNumberAsString);
                if (episodesCount == videoInfo.GetEpisodeCountBySeason(seasonNumber))
                {
                    DownloadBundle downloadBundle = new DownloadBundle
                    {
                        VideoInfo =  videoInfo,
                        IsFullSeason =  true,
                        Season =  seasonNumber,
                        TorrentLink =  torrentInfo.DownloadLink,
                        Subtitles = subtitles.GroupBy(subtitle => subtitle.Name).Select(subtitleGroup => subtitleGroup.First().DownloadLink)
                    };

                    yield return downloadBundle;
                }
            }
        }
    }
}
