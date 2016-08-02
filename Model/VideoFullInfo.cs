using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model
{
    struct VideoFullInfo
    {
        public VideoFullInfo(VideoInfo videoVideoInfo, IEnumerable<SubtitleInfo> availableSubtitles)
        {
            this.BaseVideoInfo = videoVideoInfo;
            AvailableSubtitles = availableSubtitles;
        }

        public VideoInfo BaseVideoInfo { get; }

        public IEnumerable<SubtitleInfo> AvailableSubtitles { get; }
    }
}
