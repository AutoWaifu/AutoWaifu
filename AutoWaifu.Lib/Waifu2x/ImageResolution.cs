using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    [Serializable]
    public class ImageResolution
    {
        public float Width { get; set; }
        public float Height { get; set; }

        public int WidthInt
        {
            get { return (int)Math.Round(Width); }
            set { Width = value; }
        }
        public int HeightInt
        {
            get { return (int)Math.Round(Height); }
            set { Height = value; }
        }


        public static bool operator==(ImageResolution a, ImageResolution b)
        {
            bool aIsNull = object.ReferenceEquals(a, null);
            bool bIsNull = object.ReferenceEquals(b, null);

            if (aIsNull && bIsNull)
                return true;

            if (aIsNull != bIsNull)
                return false;

            return a.WidthInt == b.WidthInt &&
                   a.HeightInt == b.HeightInt;
        }

        public static bool operator !=(ImageResolution a, ImageResolution b)
        {
            return !(a == b);
        }
    }
}
