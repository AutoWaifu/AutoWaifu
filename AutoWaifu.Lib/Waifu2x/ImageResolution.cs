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
    }
}
