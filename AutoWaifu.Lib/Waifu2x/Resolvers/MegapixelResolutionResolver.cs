using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    [Serializable]
    public class MegapixelResolutionResolver : IResolutionResolver
    {
        public MegapixelResolutionResolver()
        {

        }

        public MegapixelResolutionResolver(float desiredMegapixelCount)
        {
            DesiredMegapixels = desiredMegapixelCount;
        }

        public float DesiredMegapixels { get; set; }

        public ImageResolution Resolve(ImageResolution inputRes)
        {
            float inputMegapixels = inputRes.Width * inputRes.Height * 1e6f;
            float scale = DesiredMegapixels / inputMegapixels;

            return new ImageResolution
            {
                Width = (float)Math.Floor(scale * inputRes.Width),
                Height = (float)Math.Floor(scale * inputRes.Height)
            };
        }
    }
}
