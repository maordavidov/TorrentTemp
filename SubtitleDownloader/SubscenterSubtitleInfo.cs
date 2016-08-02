namespace SubtitleDownloader
{
    using System;

    public class SubscenterSubtitleInfo
    {
        public SubscenterSubtitleInfo()
        {

        }
        public SubscenterSubtitleInfo(string id, string subtitleName, string key, string imdbHash)
        {
            this.SubtitleId = id;
            this.SubtitleName = subtitleName;
            this.Key = key;
            ImdbHash = imdbHash;
        }

        public SubscenterSubtitleInfo(string line)
        {
            string[] strs = line.Split(new[] { "@@" }, StringSplitOptions.None);
            this.SubtitleName = strs[0];
            this.SubtitleId = strs[1];
            this.Key = strs[2];
        }

        public string SubtitleId { get; set; }

        public string SubtitleName { get; set; }

        public string Key { get; set; }

        public string ImdbHash { get; set; }
    }
}