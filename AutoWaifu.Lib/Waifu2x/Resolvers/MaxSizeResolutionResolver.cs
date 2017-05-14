using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    [Serializable]
    public class MaxSizeResolutionResolver : IResolutionResolver
    {
        public MaxSizeResolutionResolver()
        {

        }

        public MaxSizeResolutionResolver(ImageResolution maxRes)
        {
            MaxResolution = maxRes;
        }

        public ImageResolution MaxResolution { get; set; }

        public ImageResolution Resolve(ImageResolution inputRes)
        {
            float xRatio = MaxResolution.Width / inputRes.Width;
            float yRatio = MaxResolution.Height / inputRes.Height;

            float xDiff = MaxResolution.Width - inputRes.Width;
            float yDiff = MaxResolution.Height - inputRes.Height;

            if (xDiff <= 0 && yDiff <= 0)
                return inputRes;

            if (xDiff > yDiff)
            {
                return new ImageResolution
                {
                    Width = inputRes.Width * xRatio,
                    Height = inputRes.Height * xRatio
                };
            }
            else
            {
                return new ImageResolution
                {
                    Width = inputRes.Width * yRatio,
                    Height = inputRes.Height * yRatio
                };
            }
        }
    }
}
