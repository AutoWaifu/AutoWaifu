using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu2
{
    static class FileExt
    {
        public static string ReadAllText(string filePath)
        {
            using (var textFile = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new StreamReader(textFile))
            {
                return reader.ReadToEnd();
            }
        }

        public static bool TryDelete(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
