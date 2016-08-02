using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    public class DownloadBundle
    {
        public VideoInfo VideoInfo { get; set; }
        public int Season { get; set; }
        public int Eposode { get; set; }
        public bool IsFullSeason { get; set; }
        public string TorrentLink { get; set; }
        public IEnumerable<string> Subtitles { get; set; }
    }
}
