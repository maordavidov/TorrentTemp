namespace VideoInformationDownloader
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using Model;

    public class VideoInfoReceiver
    {
        private readonly HttpClient _omdbApiHttpClient = new HttpClient();

        private const string _baseAddress = "http://www.omdbapi.com";

        public async Task<VideoInfo> DownloadAsync(IMDBStruct imdbInfo)
        {
            int seasonsCount = 1;
            string title = null;

            Dictionary<int, int> seasonToEpisodeDic = new Dictionary<int, int>();

            while (true)
            {
                string allText = await this._omdbApiHttpClient.GetStringAsync($"{_baseAddress}?i={imdbInfo.Hash}&season={seasonsCount}");
                if (seasonsCount == 1)
                {
                    title = Regex.Match(allText, "\"Title\":\"(?<title>.+?)\"").Groups["title"]?.Value;
                }

                if (Regex.IsMatch(allText, "\"Response\":\"True\"") == false)
                    break;

                int episodesCount = Regex.Matches(allText, "\"Episode\":\"\\d+\",\"imdbRating\":\"\\d+.\\d+?\"").Count;

                if (episodesCount > 0)
                {
                    seasonToEpisodeDic[seasonsCount] = episodesCount;
                    seasonsCount++;
                    continue;
                }

                break;
            }

            return new VideoInfo(imdbInfo, title, seasonToEpisodeDic);
        }

    }
}
