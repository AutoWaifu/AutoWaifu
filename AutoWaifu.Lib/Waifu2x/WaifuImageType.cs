using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    [Serializable]
    [Flags]
    public enum WaifuImageType
    {
        Invalid = 0,

        Lossless    = 1 << 1,
        Lossy       = 1 << 2,
        Image       = 1 << 3,
        Animated    = 1 << 4,
        Video       = 1 << 5 | Animated | Lossy,

        Jpeg        = 1 << 10 | Lossy | Image,
        Png         = 1 << 11 | Lossless | Image,

        Gif         = 1 << 15 | Lossy | Animated,

        Mp4         = 1 << 20 | Video,
        Webm        = 1 << 21 | Video
    }
}
