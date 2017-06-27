using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    [Serializable]
    public class TargetPixelCountResolutionResolver : IResolutionResolver
    {
        public TargetPixelCountResolutionResolver()
        {

        }

        public TargetPixelCountResolutionResolver(float desiredPixelCount)
        {
            DesiredPixels = desiredPixelCount;
        }

        public float DesiredPixels { get; set; }

        public ImageResolution Resolve(ImageResolution inputRes)
        {
            float inputMegapixels = inputRes.Width * inputRes.Height;
            float scale = (float)Math.Sqrt(DesiredPixels / inputMegapixels);

            return new ImageResolution
            {
                Width = (float)Math.Floor(scale * inputRes.Width),
                Height = (float)Math.Floor(scale * inputRes.Height)
            };
        }
    }
}

