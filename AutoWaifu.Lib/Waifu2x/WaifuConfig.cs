using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    [Serializable]
    public struct WaifuConfig
    {
        public string InputPath;
        public string OutputPath;

        public WaifuConvertMode ConvertMode;

        public string TempInputDir;
        public string TempOutputDir;

        public IResolutionResolver ResolutionResolver;
    }
}
