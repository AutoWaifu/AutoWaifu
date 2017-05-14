using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu2
{
    static class AppRelativePath
    {
        /// <summary>
        /// Returns a path relative to the input folder
        /// </summary>
        public static string Create(string path)
        {

            string matchingSubString = string.Empty;
            for (int i = 0; i < path.Length; i++)
            {
                var c = path[i];

                if (
                        (i < AppSettings.Main.InputDir.Length && c == AppSettings.Main.InputDir[i]) ||
                        (i < AppSettings.Main.OutputDir.Length && c == AppSettings.Main.OutputDir[i]) ||
                        (i < AppSettings.Main.TempDir.Length && c == AppSettings.Main.TempDir[i]) ||
                        (i < AppSettings.Main.TempDirInput.Length && c == AppSettings.Main.TempDirInput[i]) ||
                        (i < AppSettings.Main.TempDirOutput.Length && c == AppSettings.Main.TempDirOutput[i])
                    )
                {
                    matchingSubString += path[i];
                }
            }

            if (matchingSubString.Length != 0)
                path = path.Replace(matchingSubString, string.Empty);

            return path.Trim(new[] { '/', '\\' });
        }

        public static string CreateInput(string path)
        {
            string relativePath = Create(path);
            return Path.Combine(AppSettings.Main.InputDir, relativePath);
        }

        public static string CreateOutput(string path)
        {
            string relativePath = Create(path);
            return Path.Combine(AppSettings.Main.OutputDir, relativePath);
        }
    }
}
