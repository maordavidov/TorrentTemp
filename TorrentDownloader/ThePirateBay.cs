using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TorrentDownloader
{
    using System.Collections;
    using System.IO;
    using System.Net.Http;
    using System.Xml;
    using System.Xml.Linq;

    using HtmlAgilityPack;

    using Model;

    public class ThePirateBay
    {
        private const string BaseAddress = "https://thepiratebay.se";
        private const string AddressFormat = "https://thepiratebay.se/search/{0}/{1}/7";
        private HttpClient _httpClient = new HttpClient();

        public async Task DownloadAsync(VideoInfo videoInfo)
        {
            for (int i = 0; i < ushort.MaxValue; i++)
            {
                Stream dataStream = await _httpClient.GetStreamAsync(String.Format(AddressFormat, videoInfo.IMDBInfo.Hash, i));
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.Load(dataStream);

                // XmlTextReader xmlTextReader = new XmlTextReader(dataStream);

                var allElementsWithClassFloat = htmlDocument.DocumentNode.SelectNodes("//*[contains(@class,'detLink')]");
                var htmlElements = allElementsWithClassFloat.Select(htmlNode =>
                                                            {
                                                                string url = htmlNode.GetAttributeValue("href", "-1");
                                                                string value = htmlNode.InnerText;
                                                                return new TorrentInfo(value, videoInfo.IMDBInfo.Hash, $"{BaseAddress}/{url}");
                                                            })
                                                            .Where(torrentInfo => torrentInfo.DownloadLink != "-1")
                                                            .ToList();


            }

        }
    }
}
