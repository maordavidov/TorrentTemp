namespace Console
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using HtmlAgilityPack;

    using Model;

    using MonoTorrent.Common;

    using SubtitleDownloader;

    using TorrentDownloader;

    using VideoInformationDownloader;

    //[BsonIgnoreExtraElements]

    //class Torec
    //{
    //    private HttpClient _httpClient;

    //    public Torec()
    //    {
    //        CookieContainer cookieContainer = new CookieContainer();
    //        cookieContainer.Add(new Uri("http://www.torec.net"), new Cookie("Torec_NC_site", "userID=74818&userp=96A983A8A57C&user=MaorD"));

    //        _httpClient = new HttpClient(new HttpClientHandler
    //        {
    //            UseCookies = true,
    //            CookieContainer = cookieContainer,
    //        })
    //        {
    //            BaseAddress = new Uri("http://www.torec.net")
    //        };

    //    }
    //    //TODO LIST: 
    //    //           send ssearch.asp POST request    V
    //    //           read series id from link         V
    //    //           send tResult request with the following: $.post("/ajax/search/tResult.asp?rnd=" + Math.random(), { id: seriesId, exp: imdbHash , cat: 3(tvshow) }) V
    //    //           after receive data - read it and get all episodes links    V
    //    //           send get request for every episode subtitles               V
    //    //           find available subtitles and send two request - one with the sub_id (available in the Do method) and the second is with the downloadun.asp which should be with the received link from the first request.
    //    //           note: the last stage (two requests) should come only when we want to download the subtitle itself. in order to add the subtitle to MongoDB we should use the download link as the one with the sub_id..&code=.. and so on


    //    public async Task Do(VideoInfo videoInfo)
    //    {
    //        Dictionary<string, string> dic = new Dictionary<string, string>
    //        {
    //            ["search"] = videoInfo.IMDBInfo.Hash
    //        };

    //        var result = await _httpClient.PostAsync("/ssearch.asp", new FormUrlEncodedContent(dic));


    //        HtmlDocument htmlDocument = new HtmlDocument();
    //        htmlDocument.Load(await result.Content.ReadAsStreamAsync());
    //        HtmlNodeCollection nodes = htmlDocument.DocumentNode.SelectNodes("//a[contains(@onclick,'Result')]");
    //        HtmlNode htmlNode = nodes.FirstOrDefault();

    //        if (htmlNode == null)
    //            throw new SeriesNotFoundException(videoInfo);

    //        string seriesLink = htmlNode.GetAttributeValue("href", "-1");
    //        string onClick = htmlNode.GetAttributeValue("onclick", "-1");
    //        //return tResult(260,'tt0903747',3,this.getAttribute('href'));
    //        Match[] matches = Regex.Matches(onClick, @"(?<value>\d+),", RegexOptions.ECMAScript).Cast<Match>().ToArray();
    //        string seriesId = matches[0].Groups["value"].Value;
    //        string category = matches[1].Groups["value"].Value;

    //        Dictionary<string, string> contentDic = new Dictionary<string, string>
    //        {
    //            ["id"] = seriesId,
    //            ["exp"] = videoInfo.IMDBInfo.Hash,
    //            ["cat"] = category
    //        };

    //        HttpResponseMessage responseMessage = await _httpClient.PostAsync($"/ajax/search/tResult.asp?rnd={new Random().NextDouble()}", new FormUrlEncodedContent(contentDic));//new { id = 1, exp = videoInfo.IMDBInfo.Hash, cat = 1});
    //        responseMessage.EnsureSuccessStatusCode();

    //        Stream linksPageStream = await _httpClient.GetStreamAsync(seriesLink);

    //        htmlDocument = new HtmlDocument();
    //        htmlDocument.Load(linksPageStream);

    //        for (int seasonNumber = 1; seasonNumber < videoInfo.SeasonsNumber; seasonNumber++)
    //        {
    //            HtmlNodeCollection collection =
    //                htmlDocument.DocumentNode.SelectNodes($"//div[starts-with(@id, 'season_{seasonNumber}')]//a[starts-with(@href, '/sub.asp?sub_id=')]");

    //            int episodesCount = videoInfo.GetEpisodeCountBySeason(seasonNumber);
    //            IEnumerable<HtmlNode> relevantNodes = collection.Take(episodesCount);

    //            IEnumerable<string> linksToEpisodes = relevantNodes.Select(node => node.GetAttributeValue("href", "-1"));

    //            foreach (string linkToEpisode in linksToEpisodes) //TODO: this is extract episodes...
    //            {
    //                string episodeId = Regex.Match(linkToEpisode, @"(\d+)").Groups[0].Value;
    //                Stream versionsPageStream = await _httpClient.GetStreamAsync(linkToEpisode);
    //                HtmlDocument versionsPageDocument = new HtmlDocument();
    //                versionsPageDocument.Load(versionsPageStream);
    //                HtmlNodeCollection subtitleVersionOptions = versionsPageDocument.DocumentNode.SelectNodes("//select[starts-with(@id,'download_version')]//option");
    //                HtmlNodeCollection fullVersionsNamesCollection = versionsPageDocument.DocumentNode.SelectNodes("//*[starts-with(@id,'version_list')]/span");
    //                List<string> fullVersionsNames =
    //                    fullVersionsNamesCollection.Select(n => n.InnerText.Trim('\n').Trim()).ToList();

    //                IEnumerable<KeyValuePair<string, string>> subtitlePair = subtitleVersionOptions.Select(
    //                    (node, index) =>
    //                        {
    //                           // string subtitleName = node.NextSibling.InnerText.Trim().Replace(" ", ".");
    //                            string subtitleFullName = fullVersionsNames[index];
    //                            //if (fullVersionsNames[index].EndsWith(subtitleName) == false)
    //                            //{
    //                            //    subtitleFullName = fullVersionsNames.First(sub => sub.EndsWith(subtitleName));
    //                            //}

    //                            string subtitleHash = node.GetAttributeValue("value", null);
    //                            string subtitleBodyLink = $"sub_id={episodeId}&code={subtitleHash}&sh=yes&guest=&timewaited=-1";
    //                            return new KeyValuePair<string, string>(subtitleFullName, subtitleBodyLink);
    //                        }).ToList();
    //                //TODO: use subtitlePair links as displayed below (split them into FormUrlEncodedContent and so on)
    //            }
    //        }

    //        var v = "sub_id=13160&code=8FCA9E9DB4919784A6A99EDC8FA9A8B8C597C69EA59591A8B8C4B3BAC2AAB3B2&sh=yes&guest=&timewaited=-1"
    //            .Split('&')
    //            .Select(s => s.Split('='))
    //            .Select(arr => new KeyValuePair<string, string>(arr[0], arr[1]));

    //        result = _httpClient.PostAsync("/ajax/sub/downloadun.asp", new FormUrlEncodedContent(v)).Result;

    //        string content = result.Content.ReadAsStringAsync().Result;

    //        result = _httpClient.GetAsync(content).Result;

    //        using (FileStream fs = File.OpenWrite(@"d:\kuku.zip"))
    //            result.Content.CopyToAsync(fs).Wait();


    //    }
    //}

    internal class SeriesNotFoundException : Exception
    {
        public SeriesNotFoundException(VideoInfo info)
        {
            VideoInfo = info;
        }

        public VideoInfo VideoInfo { get; }
    }

    class Program
    {

        static void Main(string[] args)
        {
            string imdbUrl = "http://www.imdb.com/title/tt2193021";
            //string imdbUrl = "http://www.imdb.com/title/tt0903747/";
            IMDBStruct imdbStruct = new IMDBStruct(imdbUrl);
            VideoInfoReceiver downloader = new VideoInfoReceiver();
            VideoInfo info = downloader.DownloadAsync(imdbStruct).Result;

            new Torec().DownloadSubtitlesInfoAsync(info).Wait();

            return;
            Torrent torrent = Torrent.Load(@"D:\Breaking.Bad.S01.720p.HDTV.x264.RoSubbed-FL.torrent");

            // return;

            //string imdbUrl = "http://www.imdb.com/title/tt2193021";


            FileListDownloader fileListDownloader = new FileListDownloader();
            //Task downloadTask = fileListDownloader.DownloadMetadataAsync(videoInfo: info);

            //SubscenterDownloader subscenterDownloader = new SubscenterDownloader();
            //Task subscenterTask = subscenterDownloader.DownloadAsync(videoInfo: info);
            //Task.WaitAll(downloadTask, subscenterTask);

            fileListDownloader.DownloaTorrent(videoInfo: info);
        }
    }
}
