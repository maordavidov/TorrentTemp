using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public struct VideoInfo
    {
        private readonly Dictionary<int, int> _seasonToEpisodeDic;

        public VideoInfo(IMDBStruct imdbInfo, string title, Dictionary<int, int> seasonToEpisodeDic)
        {
            this.SeasonsNumber = seasonToEpisodeDic.Count;
            this._seasonToEpisodeDic = seasonToEpisodeDic;
            this.IMDBInfo = imdbInfo;
            this.Title = title;
        }

        public IMDBStruct IMDBInfo { get; }
        public string Title { get; }

        public int SeasonsNumber { get; }

        public int GetEpisodeCountBySeason(int seasonNumber)
        {
            return this._seasonToEpisodeDic[seasonNumber];
        }
    }
}
