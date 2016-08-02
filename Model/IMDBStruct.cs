namespace Model
{
    using System.Text.RegularExpressions;

    public struct IMDBStruct
    {
        public IMDBStruct(string imdbLink)
        {
            this.Link = imdbLink;
            Match hashMatch = Regex.Match(imdbLink, "title/(?<hash>.*)");
            this.Hash = hashMatch.Groups["hash"].Value.TrimEnd('/');
        }

        public string Link { get; }

        public string Hash { get; }

        public static implicit operator string(IMDBStruct imdb)
        {
            return imdb.Hash;
        }
    }
}
