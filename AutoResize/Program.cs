using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoResize
{
    //  Resizes an image to be have even width/height for ffmpeg
    class Program
    {
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

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

            return destImage;
        }

        [STAThread]
        static void Main(string[] args)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.InitialDirectory = Directory.GetCurrentDirectory();
            ofd.Multiselect = true;

            if (ofd.ShowDialog() != DialogResult.OK)
                return;

            Parallel.ForEach(ofd.FileNames, (file) =>
            {
                var path = Path.GetFullPath(Path.GetDirectoryName(file));
                var outputPath = Path.Combine(path, "scaled");
                if (!Directory.Exists(outputPath))
                    Directory.CreateDirectory(outputPath);


                using (var img = Image.FromFile(file))
                {
                    int newWidth = img.Width;
                    int newHeight = img.Height;

                    newWidth += newWidth % 2;
                    newHeight += newHeight % 2;

                    if (newWidth == img.Width && newHeight == img.Height)
                        return;

                    using (var scaled = ResizeImage(img, newWidth, newHeight))
                    {
                        scaled.Save(Path.Combine(outputPath, Path.GetFileName(file)), ImageFormat.Jpeg);
                    }
                }
            });
        }
    }
}
