using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    [Serializable]
    public enum WaifuConvertMode
    {
        CPU,
        GPU,
        cuDNN
    }
}
