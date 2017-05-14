using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    [Serializable]
    public class ScaleResolutionResolver : IResolutionResolver
    {
        public ScaleResolutionResolver()
        {

        }

        public ScaleResolutionResolver(float scale)
        {
            ScaleFactor = scale;
        }

        public float ScaleFactor { get; set; }

        public ImageResolution Resolve(ImageResolution inputRes)
        {
            return new ImageResolution
            {
                Width = ScaleFactor * inputRes.Width,
                Height = ScaleFactor * inputRes.Height
            };
        }
    }
}
