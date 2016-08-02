namespace Model
{
    struct SubtitleDiskPath
    {
        private readonly string _path;

        public SubtitleDiskPath(string subtitlePath)
        {
            this._path = subtitlePath;
        }


        public static implicit operator SubtitleDiskPath(string s)
        {
            return new SubtitleDiskPath(s) ;
        }

        public static explicit operator string(SubtitleDiskPath s)
        {
            return s._path;
        }
    }
}
