using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SubtitleDownloader
{
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks.Dataflow;

    using HtmlAgilityPack;

    using Model;

    internal struct SearchInfo
    {
        public SearchInfo(VideoInfo videoInfo, Stream searchStream)
        {
            VideoInfo = videoInfo;
            SearchStream = searchStream;
        }

        public VideoInfo VideoInfo { get; }

        public Stream SearchStream { get; }
    }

    internal struct SeriesInfo
    {
        public SeriesInfo(string id, string searchTerm, string category, VideoInfo videoInfo, string pageLink)
        {
            Id = id;
            SearchTerm = searchTerm;
            Category = category;
            VideoInfo = videoInfo;
            PageLink = pageLink;
        }

        public string PageLink { get; }

        public string Id { get; }

        public string SearchTerm { get; }

        public string Category { get; }

        public VideoInfo VideoInfo { get; }
    }

    internal struct SeriesPageInfo
    {
        public SeriesPageInfo(VideoInfo videoInfo, Stream linksPageAsStream)
        {
            LinksPageStream = linksPageAsStream;
            VideoInfo = videoInfo;
        }
        public VideoInfo VideoInfo { get; }

        public Stream LinksPageStream { get; }
    }

    internal struct SeasonInfo
    {
        public SeasonInfo(VideoInfo videoInfo, int seasonNumber, IEnumerable<string> episodesLinks)
        {
            VideoInfo = videoInfo;
            SeasonNumber = seasonNumber;
            EpisodesLinks = episodesLinks;
        }
        public int SeasonNumber { get; set; }
        public IEnumerable<string> EpisodesLinks { get; set; }
        public VideoInfo VideoInfo { get; set; }
    }

    internal struct EpisodeInfo
    {
        public EpisodeInfo(VideoInfo videoInfo, string episodeId, string episodeLink)
        {
            VideoInfo = videoInfo;
            EpisodeLink = episodeLink;
            EpisodeId = episodeId;
        }
        public string EpisodeLink { get; }

        public string EpisodeId { get; }

        public VideoInfo VideoInfo { get; }
    }

    internal struct SubtitleVersionInfo
    {
        public SubtitleVersionInfo(VideoInfo videoInfo, string subtitleName, Dictionary<string, string> postBodyData)
        {
            VideoInfo = videoInfo;
            SubtitleName = subtitleName;
            PostBodyData = postBodyData;
        }

        public string SubtitleName { get; }

        public Dictionary<string, string> PostBodyData { get; }

        public VideoInfo VideoInfo { get; }
    }



    public class Torec
    {
        private HttpClient _httpClient;

        private TransformBlock<VideoInfo, SearchInfo> _searchBlock;

        private TransformBlock<SearchInfo, SeriesInfo> _extractSeriesFromSearch;

        private TransformBlock<SeriesInfo, SeriesPageInfo> _downloadSeriesPage;

        private TransformManyBlock<SeriesPageInfo, SeasonInfo> _extractSeriesEpisodesBySeason;

        private TransformManyBlock<SeasonInfo, EpisodeInfo> _extractEpisodes;

        private TransformManyBlock<EpisodeInfo, SubtitleVersionInfo> _getEpisodePageAndExtractAvailableVersions;

        private ActionBlock<SubtitleVersionInfo> _trash = new ActionBlock<SubtitleVersionInfo>(
            (subtitleVersionInfo) =>
                {
                    Console.WriteLine($"subtitle name {subtitleVersionInfo.SubtitleName}");
                });

        public Torec()
        {
            CookieContainer cookieContainer = new CookieContainer();
            cookieContainer.Add(new Uri("http://www.torec.net"), new Cookie("Torec_NC_site", "userID=74818&userp=96A983A8A57C&user=MaorD"));

            _httpClient = new HttpClient(new HttpClientHandler
            {
                UseCookies = true,
                CookieContainer = cookieContainer,
            })
            {
                BaseAddress = new Uri("http://www.torec.net")
            };



            _searchBlock = new TransformBlock<VideoInfo, SearchInfo>(videoInfo => SearchAsync(videoInfo));
            _extractSeriesFromSearch = new TransformBlock<SearchInfo, SeriesInfo>(searchInfo => ExtractSeriesFromSearch(searchInfo));
            _downloadSeriesPage = new TransformBlock<SeriesInfo, SeriesPageInfo>(seriesInfo => DownloadSeriesPageAsync(seriesInfo));
            _extractSeriesEpisodesBySeason = new TransformManyBlock<SeriesPageInfo, SeasonInfo>(seriesPageInfo => ExtractSeasons(seriesPageInfo));
            _extractEpisodes = new TransformManyBlock<SeasonInfo, EpisodeInfo>(seasonInfo => ExtractEpisodesFromSeasonInfo(seasonInfo));
            _getEpisodePageAndExtractAvailableVersions = new TransformManyBlock<EpisodeInfo, SubtitleVersionInfo>(episodeInfo => DownloadEpisodePageAndExtractAvailableVersionsAsync(episodeInfo), new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = -1 });


            _searchBlock.LinkTo(_extractSeriesFromSearch, new DataflowLinkOptions { PropagateCompletion = true });
            _extractSeriesFromSearch.LinkTo(_downloadSeriesPage, new DataflowLinkOptions { PropagateCompletion = true });
            _downloadSeriesPage.LinkTo(_extractSeriesEpisodesBySeason, new DataflowLinkOptions { PropagateCompletion = true });
            _extractSeriesEpisodesBySeason.LinkTo(_extractEpisodes, new DataflowLinkOptions { PropagateCompletion = true });
            _extractEpisodes.LinkTo(_getEpisodePageAndExtractAvailableVersions, new DataflowLinkOptions { PropagateCompletion = true });
            _getEpisodePageAndExtractAvailableVersions.LinkTo(_trash, new DataflowLinkOptions { PropagateCompletion = true });

        }

        private async Task<SearchInfo> SearchAsync(VideoInfo videoInfo)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>
            {
                ["search"] = videoInfo.IMDBInfo.Hash
            };

            var result = await _httpClient.PostAsync("/ssearch.asp", new FormUrlEncodedContent(dic));
            Stream searchResultStream = await result.Content.ReadAsStreamAsync();
            return new SearchInfo(videoInfo: videoInfo, searchStream: searchResultStream);
        }

        private SeriesInfo ExtractSeriesFromSearch(SearchInfo searchInfo)
        {
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.Load(searchInfo.SearchStream);
            HtmlNodeCollection nodes = htmlDocument.DocumentNode.SelectNodes("//a[contains(@onclick,'Result')]");
            HtmlNode htmlNode = nodes.FirstOrDefault();

            if (htmlNode == null)
                throw new SeriesNotFoundException(searchInfo.VideoInfo);

            string seriesLink = htmlNode.GetAttributeValue("href", "-1");
            string onClick = htmlNode.GetAttributeValue("onclick", "-1");
            //return tResult(260,'tt0903747',3,this.getAttribute('href'));
            Match[] matches = Regex.Matches(onClick, @"(?<value>\d+),", RegexOptions.ECMAScript).Cast<Match>().ToArray();
            string seriesId = matches[0].Groups["value"].Value;
            string category = matches[1].Groups["value"].Value;

            string searchTerm = searchInfo.VideoInfo.IMDBInfo.Hash;

            return new SeriesInfo(seriesId, searchTerm, category, searchInfo.VideoInfo, seriesLink);
        }

        private async Task<SeriesPageInfo> DownloadSeriesPageAsync(SeriesInfo seriesInfo)
        {
            Dictionary<string, string> contentDic = new Dictionary<string, string>
            {
                ["id"] = seriesInfo.Id,
                ["exp"] = seriesInfo.SearchTerm,
                ["cat"] = seriesInfo.Category
            };

            HttpResponseMessage responseMessage = await _httpClient.PostAsync($"/ajax/search/tResult.asp?rnd={new Random().NextDouble()}", new FormUrlEncodedContent(contentDic));//new { id = 1, exp = videoInfo.IMDBInfo.Hash, cat = 1});
            responseMessage.EnsureSuccessStatusCode();
            Stream linksPageStream = await _httpClient.GetStreamAsync(seriesInfo.PageLink);
            return new SeriesPageInfo(videoInfo: seriesInfo.VideoInfo, linksPageAsStream: linksPageStream);
        }

        private IEnumerable<SeasonInfo> ExtractSeasons(SeriesPageInfo seriesPageInfo)
        {
            VideoInfo videoInfo = seriesPageInfo.VideoInfo;
            HtmlDocument htmlDocument = new HtmlDocument();

            htmlDocument.Load(seriesPageInfo.LinksPageStream);

            for (int seasonNumber = 1; seasonNumber <= videoInfo.SeasonsNumber; seasonNumber++)
            {
                HtmlNodeCollection collection =
                    htmlDocument.DocumentNode.SelectNodes(
                        $"//div[starts-with(@id, 'season_{seasonNumber}')]//a[starts-with(@href, '/sub.asp?sub_id=')]");

                int episodesCount = videoInfo.GetEpisodeCountBySeason(seasonNumber);
                IEnumerable<HtmlNode> relevantNodes = collection.Take(episodesCount);

                IEnumerable<string> linksToEpisodes = relevantNodes.Select(node => node.GetAttributeValue("href", "-1"));
                yield return new SeasonInfo(videoInfo: videoInfo, seasonNumber: seasonNumber, episodesLinks: linksToEpisodes);
            }
        }

        private IEnumerable<EpisodeInfo> ExtractEpisodesFromSeasonInfo(SeasonInfo seasonInfo)
        {
            foreach (string episodeLink in seasonInfo.EpisodesLinks)
            {
                string episodeId = Regex.Match(episodeLink, @"(\d+)").Groups[0].Value;
                yield return new EpisodeInfo(videoInfo: seasonInfo.VideoInfo, episodeId: episodeId, episodeLink: episodeLink);
            }
        }

        private async Task<IEnumerable<SubtitleVersionInfo>> DownloadEpisodePageAndExtractAvailableVersionsAsync(EpisodeInfo episodeInfo)
        {
            Stream episodeVersionsPageStream = await _httpClient.GetStreamAsync(episodeInfo.EpisodeLink);
            HtmlDocument versionsPageDocument = new HtmlDocument();
            versionsPageDocument.Load(episodeVersionsPageStream);
            HtmlNodeCollection subtitleVersionOptions = versionsPageDocument.DocumentNode.SelectNodes("//select[starts-with(@id,'download_version')]//option");
            HtmlNodeCollection fullVersionsNamesCollection = versionsPageDocument.DocumentNode.SelectNodes("//*[starts-with(@id,'version_list')]/span");
            List<string> fullVersionsNames =
                fullVersionsNamesCollection.Select(n => n.InnerText.Trim('\n').Trim()).ToList();

            IEnumerable<SubtitleVersionInfo> subtitleVersionInfos = subtitleVersionOptions.Select((node, index) =>
                {
                    // string subtitleName = node.NextSibling.InnerText.Trim().Replace(" ", ".");
                    string subtitleFullName = fullVersionsNames[index];
                    //if (fullVersionsNames[index].EndsWith(subtitleName) == false)
                    //{
                    //    subtitleFullName = fullVersionsNames.First(sub => sub.EndsWith(subtitleName));
                    //}

                    string subtitleHash = node.GetAttributeValue("value", null);
                    Dictionary<string, string> subtitleBodyDataDic = new Dictionary<string, string>
                    {
                        ["sub_id"] = episodeInfo.EpisodeId,
                        ["code"] = subtitleHash,
                        ["sh"] = "yes",
                        ["guest"] = String.Empty,
                        ["timewaited"] = "-1"
                    }; ///$"sub_id={episodeInfo.EpisodeId}&code={subtitleHash}&sh=yes&guest=&timewaited=-1";
                    // return new KeyValuePair<string, string>(subtitleFullName, subtitleBodyLink);
                    return new SubtitleVersionInfo(videoInfo: episodeInfo.VideoInfo, subtitleName: subtitleFullName, postBodyData: subtitleBodyDataDic);
                });

            return subtitleVersionInfos;
        }


        public async Task DownloadSubtitlesInfoAsync(VideoInfo videoInfo)
        {
            await _searchBlock.SendAsync(videoInfo);
            _searchBlock.Complete();
            await _trash.Completion; //TODO
        }
    }

    internal class SeriesNotFoundException : Exception
    {
        public SeriesNotFoundException(VideoInfo videoInfo)
        {

        }
    }
}
