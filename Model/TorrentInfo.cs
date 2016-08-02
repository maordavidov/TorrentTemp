namespace Model
{
    public class TorrentInfo
    {
        public TorrentInfo()
        {

        }

        public TorrentInfo(string torrentName, string imdbHash, string downloadLink)
        {
            this.TorrentName = torrentName;
            this.DownloadLink = downloadLink;
            this.ImdbHash = imdbHash;
        }

        public string TorrentName { get; set; }

        public string ImdbHash { get; set; }

        public string DownloadLink { get; set; }

    }
}
