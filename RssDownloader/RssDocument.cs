using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RssDownloader
{
    using System.Text.RegularExpressions;
    using System.Xml.Linq;
    using System.Xml.XPath;

    class RssDocument
    {
        public IEnumerable<RssFeed> GenerateFeeds(XDocument rssDocument)
        {
            IEnumerable<XElement> items = rssDocument.XPathSelectElements("/rss/channel/item");

            foreach (XElement item in items)
            {
                XElement titleElement = item.Element("title");
                XElement linkElement = item.Element("link");
                XElement descriptionElement = item.Element("description");

                string description = descriptionElement.Value;
                Match match = Regex.Match(description, @"imdb\.com/title(?<imdb>.*?)"">");

                RssFeed rssFeed = new RssFeed(titleElement.Value, linkElement.Value, match.Groups["imdb"].Value.Trim('/'));
                if (String.IsNullOrEmpty(rssFeed.ImdbHashCode))
                    continue;

                yield return rssFeed;
            }
        }
    }
}
