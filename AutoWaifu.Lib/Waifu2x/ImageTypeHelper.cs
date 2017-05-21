using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.Waifu2x
{
    static class ImageTypeHelper
    {
        public static string RectifyImageExtension(string imagePath)
        {
            string newPath = null;

            using (var file = File.OpenRead(imagePath))
            {
                var bytes = new byte[50];
                file.Read(bytes, 0, bytes.Length);

                var imagePathWithoutExtension = Path.Combine(Path.GetDirectoryName(imagePath), Path.GetFileNameWithoutExtension(imagePath));

                var asText = Encoding.ASCII.GetString(bytes).ToLower();
                if (asText.Contains("jfif"))
                    newPath = imagePathWithoutExtension + ".jpeg";

                if (asText.Contains("png"))
                    newPath = imagePathWithoutExtension + ".png";
            }

            if (newPath == null)
                return null;

            if (newPath != imagePath && !File.Exists(newPath))
                File.Move(imagePath, newPath);

            return newPath;
        }
    }
}
