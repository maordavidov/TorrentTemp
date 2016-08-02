namespace SubtitleDownloader
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Threading.Tasks.Dataflow;
    using Model;
    using MongoDBProxy;

    public class SubscenterDownloader
    {
        private readonly HttpClient _httpClient;

        private readonly SubtitleCollectionProxy _subtitlesCollection;

        private readonly TransformManyBlock<VideoInfo, Tuple<string,string>> _subtitleLinksGeneratorBlock;

        private readonly TransformBlock<Tuple<string, string>, Tuple<string, string>> _downloadAvailableSubtitlesBlock;

        private readonly TransformManyBlock<Tuple<string, string>, SubscenterSubtitleInfo> _extractAvailableSubtitles;

        private readonly ActionBlock<SubscenterSubtitleInfo> _writeFileBlock;

        public SubscenterDownloader()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://subscenter.cinemast.com/he")
            };

            _subtitlesCollection = new SubtitleCollectionProxy();

            ExecutionDataflowBlockOptions executionOptionsUnbounded = new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = -1
            };
            this._subtitleLinksGeneratorBlock = new TransformManyBlock<VideoInfo, Tuple<string, string>>(videoInfo => this.SubtitlesInfosGenerator(videoInfo));
            this._downloadAvailableSubtitlesBlock = new TransformBlock<Tuple<string, string>, Tuple<string, string>>(subtitleLink => this.DownloadAvailableSubtitles(subtitleLink), executionOptionsUnbounded);
            this._extractAvailableSubtitles = new TransformManyBlock<Tuple<string, string>, SubscenterSubtitleInfo>(availableSubtitlesRaw => this.ExtractSubscenterSubtitleInfos(availableSubtitlesRaw));
            this._writeFileBlock = new ActionBlock<SubscenterSubtitleInfo>(subtitleInfo => this.WriteToDatabase(subtitleInfo));


            this._subtitleLinksGeneratorBlock.LinkTo(this._downloadAvailableSubtitlesBlock, new DataflowLinkOptions { PropagateCompletion = true });
            this._downloadAvailableSubtitlesBlock.LinkTo(this._extractAvailableSubtitles, new DataflowLinkOptions { PropagateCompletion = true });
            this._extractAvailableSubtitles.LinkTo(this._writeFileBlock, new DataflowLinkOptions { PropagateCompletion = true });
        }

        private IEnumerable<Tuple<string,string>> SubtitlesInfosGenerator(VideoInfo videoInfo)
        {
            Console.WriteLine("About to generate subtitles for series: '{0}' seasons: {1}", videoInfo.Title, videoInfo.SeasonsNumber);
            string seriesNameForLink = videoInfo.Title.Replace(" ", "-").ToLower();

            for (int seasonNumber = 1; seasonNumber <= videoInfo.SeasonsNumber; seasonNumber++)
            {
                Console.WriteLine("generate subtitles for season number '{0}'", seasonNumber);

                int episodesForSeason = videoInfo.GetEpisodeCountBySeason(seasonNumber);
                for (int episodeNumber = 1; episodeNumber <= episodesForSeason; episodeNumber++)
                {
                    string link = $"cinemast/data/series/sb/{seriesNameForLink}/{seasonNumber}/{episodeNumber}";
                    Console.WriteLine("Generate link " + link);
                    yield return Tuple.Create(videoInfo.IMDBInfo.Hash, link);
                }
            }
        }


        private async Task<Tuple<string, string>> DownloadAvailableSubtitles(Tuple<string, string> tuple)//string subtitleLink)
        {
            string imdbHash = tuple.Item1;
            string subtitleLink = tuple.Item2;

            Console.WriteLine("About to download from '{0}'", subtitleLink);
            string availableSubtitlesRaw = await this._httpClient.GetStringAsync(subtitleLink);
            Console.WriteLine("After download from '{0}'", subtitleLink);
            return Tuple.Create(imdbHash, availableSubtitlesRaw);
        }

        private IEnumerable<SubscenterSubtitleInfo> ExtractSubscenterSubtitleInfos(Tuple<string,string> tuple)
        {
            string imdbHash = tuple.Item1;
            string httpResponse = tuple.Item2;

            Console.WriteLine("before extract subscenter subtitles info");
            IEnumerable<string> ids =
                Regex.Matches(httpResponse, "\"id\": (?<id>\\d+)")
                    .OfType<Match>()
                    .Select(m => m.Groups["id"]?.Value);

            IEnumerable<string> subtitleVersions =
                Regex.Matches(httpResponse, "\"subtitle_version\": \"(?<subtitleVersion>.+?)\"")
                    .OfType<Match>()
                    .Select(m => m.Groups["subtitleVersion"]?.Value);

            IEnumerable<string> key = Regex.Matches(httpResponse, "\"key\": \"(?<key>.+?)\"").OfType<Match>().Select(m => m.Groups["key"]?.Value);

            var result = this.CreateInfoObject(ids, subtitleVersions, key, imdbHash);
            Console.WriteLine("after extract subscenter subtitles info");
            return result;
        }


        //private async Task<SubtitleInfo> DownloadAndSaveSubtitles(SubscenterSubtitleInfo subtitleInfo)
        //{
        //    string url = $"subtitle/download/he/?sub_id={subtitleInfo.SubtitleId}&v={subtitleInfo.SubtitleName}&key={subtitleInfo.Key}";

        //    Console.WriteLine("about to download subtitles: '{0}'", subtitleInfo.SubtitleName);
        //    using (Stream zippedStream = await this._httpClient.GetStreamAsync(url))
        //    {
        //        Console.WriteLine("after to download subtitles: '{0}'", subtitleInfo.SubtitleName);

        //        string zipFilePath = Path.Combine("ZipFiles", $"{subtitleInfo.SubtitleName}.zip");
        //        using (FileStream fileStream = File.OpenWrite(zipFilePath))
        //        {
        //            Console.WriteLine($"save zip into file: '{zipFilePath}'");
        //            await zippedStream.CopyToAsync(fileStream);
        //        }

        //        return new SubtitleInfo(subtitleInfo.SubtitleName, zipFilePath);
        //    }
        //}

        private async Task WriteToDatabase(SubscenterSubtitleInfo subscenterSubtitleInfo)
        {
            Console.WriteLine($"about to appand subtitle name '{subscenterSubtitleInfo.SubtitleName}' to meta file");
            string relativeUrl = $"subtitle/download/he/?sub_id={subscenterSubtitleInfo.SubtitleId}&v={subscenterSubtitleInfo.SubtitleName}&key={subscenterSubtitleInfo.Key}";
            string fullUrl = $"{_httpClient.BaseAddress.AbsoluteUri}/{relativeUrl}";

            SubtitleInfo subtitleInfo = new SubtitleInfo(nameof(SubscenterDownloader), subscenterSubtitleInfo.SubtitleName.ToLower(), subscenterSubtitleInfo.ImdbHash, fullUrl);

            await _subtitlesCollection.InsertOneAsync(subtitleInfo);

            Console.WriteLine($"after appand subtitle name '{subscenterSubtitleInfo.SubtitleName}' to meta file");
        }


        private async Task WriteToFile(SubtitleInfo subtitleInfo)
        {
            if (File.Exists("ZipFiles/info.txt") == false)
            {
                File.Create("ZipFiles/info.txt").Dispose();
            }
            Console.WriteLine($"about to appand subtitle name '{subtitleInfo.Name}' to meta file");
            using (FileStream fs = new FileStream("ZipFiles/info.txt", FileMode.Append))
            {
                string line = $"{subtitleInfo.Name}@@{subtitleInfo.DownloadLink}\r\n";
                byte[] bytes = Encoding.Default.GetBytes(line);
                await fs.WriteAsync(bytes, 0, bytes.Length);

                Console.WriteLine($"after appand subtitle name '{subtitleInfo.Name}' to meta file");
            }
        }

        public async Task DownloadAsync(VideoInfo videoInfo)
        {
            //if (Directory.Exists("ZipFiles") == false)
            //    Directory.CreateDirectory("ZipFiles");

            //if (File.Exists("ZipFiles/info.txt") == false)
            //{
            //    File.Create("ZipFiles/info.txt").Dispose();
            //}

            Stopwatch stopWatch = Stopwatch.StartNew();

            if(await _subtitlesCollection.AnyAsync(videoInfo) == true)
            {
                Console.WriteLine("subtitles already exists in database");
                Console.WriteLine("download subtitles completed with time: '{0}'", TimeSpan.FromMilliseconds(stopWatch.ElapsedMilliseconds));
                return;
            }

            await this._subtitleLinksGeneratorBlock.SendAsync(videoInfo);
            this._subtitleLinksGeneratorBlock.Complete();

            await this._writeFileBlock.Completion;

            Console.WriteLine("download subtitles completed with time: '{0}'", TimeSpan.FromMilliseconds(stopWatch.ElapsedMilliseconds));
            //return File.ReadLines("ZipFiles/info.txt").Select(line => new SubscenterSubtitleInfo(line));
        }



        private IEnumerable<SubscenterSubtitleInfo> CreateInfoObject(IEnumerable<string> ids, IEnumerable<string> subtitleVersions, IEnumerable<string> keys, string imdbHash)
        {
            IEnumerator<string> idsEnumerator = ids.GetEnumerator();
            IEnumerator<string> subtitleVersionsEnumerator = subtitleVersions.GetEnumerator();
            IEnumerator<string> keysEnumerator = keys.GetEnumerator();

            while (idsEnumerator.MoveNext() && subtitleVersionsEnumerator.MoveNext() && keysEnumerator.MoveNext())
            {
                string id = idsEnumerator.Current;
                string subtitleVersion = subtitleVersionsEnumerator.Current;
                string key = keysEnumerator.Current;

                yield return new SubscenterSubtitleInfo(id, subtitleVersion, key, imdbHash);
            }
        }
    }
}
