using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    [Serializable]
    public enum WaifuImageType
    {
        Invalid = 0,

        Lossless = 0x0001,
        Lossy = 0x0002,

        Jpeg = 0x0010 | Lossy,
        Png = 0x0020 | Lossless,
        Gif = 0x0040 | Lossy
    }
}
