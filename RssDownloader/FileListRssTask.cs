using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssDownloader
{
    using System.IO;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.XPath;

    using Model;

    public class FileListRssTask
    {
        private readonly HttpClient _httpClient;

        public FileListRssTask()
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://filelist.ro/")
            };
        }

        public async Task DownloadAsync()
        {
            Stream dataStream = await _httpClient.GetStreamAsync("rss.php?feed=dl&cat=21&passkey=ce312e4875f9245f3bd4da0d4cb07a30");

            XDocument xDocument = XDocument.Load(new XmlTextReader(dataStream));

            RssDocument rssDocument = new RssDocument();

            IEnumerable<RssFeed> rssFeed = rssDocument.GenerateFeeds(xDocument);

            //TODO: check if something exists on registered database...
            //TODO: if do - download it..
        }
    }
}
