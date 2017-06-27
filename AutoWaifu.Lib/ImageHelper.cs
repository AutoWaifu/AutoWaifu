using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib
{
    public static class ImageHelper
    {
        public static ImageResolution GetImageResolution(string imagePath)
        {
            using (var image = Image.FromFile(imagePath))
            {
                return new ImageResolution
                {
                    Width = image.Width,
                    Height = image.Height
                };
            }
        }

        public static void ResizeImage(string imagePath, ImageResolution newSize)
        {
            // https://stackoverflow.com/questions/1922040/resize-an-image-c-sharp

            var destImage = new Bitmap(newSize.WidthInt, newSize.HeightInt);

            using (var image = Image.FromFile(imagePath))
            {
                var destRect = new Rectangle(0, 0, newSize.WidthInt, newSize.HeightInt);

                destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

                using (var graphics = Graphics.FromImage(destImage))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                    using (var wrapMode = new ImageAttributes())
                    {
                        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                        graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                    }
                }
            }

            destImage.Save(imagePath);
        }
    }
}
