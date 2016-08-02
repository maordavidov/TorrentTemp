namespace Model
{
    public class SubtitleInfo
    {
        public SubtitleInfo()
        {
            
        }
        public SubtitleInfo(string source, string subtitleName, string imdbHash, string downloadLink)
        {
            Source = source;
            this.Name = subtitleName;
            this.DownloadLink = downloadLink;
            ImdbHash = imdbHash;
        }

        public string Source { get; set; }

        public string Name { get; set; }

        public string ImdbHash { get; set; }

        public string DownloadLink { get; set; }
    }
}
