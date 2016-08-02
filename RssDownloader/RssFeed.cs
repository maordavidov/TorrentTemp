namespace RssDownloader
{
    internal struct RssFeed
    {
        public RssFeed(string name, string downloadLink, string imdbHashCode)
        {
            Name = name;
            DownloadLink = downloadLink;
            ImdbHashCode = imdbHashCode;
        }

        public string Name { get; }

        public string DownloadLink { get; }

        public string ImdbHashCode { get; }
    }
}