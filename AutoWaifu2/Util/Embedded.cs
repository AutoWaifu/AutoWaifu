using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu2
{
    static class Embedded
    {
        public static string GetTextFile(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"AutoWaifu2.{fileName}";

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
