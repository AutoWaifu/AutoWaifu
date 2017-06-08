using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoWaifu2
{
    static class FileSystemHelper
    {
        public static string AnonymizeFilePaths(string stringContainingPaths)
        {
            var filePathRegex = new Regex(@"\w+\:(?:\\\\?[\w\s\.\%\-]+)*");
            var matches = filePathRegex.Matches(stringContainingPaths);

            foreach (Match match in matches)
                stringContainingPaths = stringContainingPaths.Replace(match.Value, Path.GetFileName(match.Value));

            return stringContainingPaths;
        }

        public static void RecursiveMove(string source, string target)
        {
            Dictionary<string, string> inputOutputMap = new Dictionary<string, string>();

            if (File.Exists(source))
            {
                inputOutputMap.Add(source, target);
            }
            else
            {
                var filesInSource = Directory.EnumerateFiles(source);
                if (!Directory.Exists(target))
                    Directory.CreateDirectory(target);

                foreach (var file in filesInSource)
                {
                    inputOutputMap.Add(file, Path.Combine(target, Path.GetFileName(file)));
                }

                foreach (var directory in Directory.EnumerateDirectories(source))
                {
                    RecursiveMove(directory, Path.Combine(target, Path.GetFileName(directory)));
                }
            }

            foreach (var moveMap in inputOutputMap)
            {
                if (moveMap.Key.ToLower() == moveMap.Value.ToLower())
                    continue;

                if (File.Exists(moveMap.Value))
                    File.Delete(moveMap.Key);
                else
                    File.Move(moveMap.Key, moveMap.Value);
            }
        }

        public static void RecursiveCopy(string source, string target)
        {
            Dictionary<string, string> inputOutputMap = new Dictionary<string, string>();

            if (File.Exists(source))
            {
                inputOutputMap.Add(source, target);
            }
            else
            {
                var filesInSource = Directory.EnumerateFiles(source);
                if (!Directory.Exists(target))
                    Directory.CreateDirectory(target);

                foreach (var file in filesInSource)
                {
                    inputOutputMap.Add(file, Path.Combine(target, Path.GetFileName(file)));
                }

                foreach (var directory in Directory.EnumerateDirectories(source))
                {
                    RecursiveMove(directory, Path.Combine(target, Path.GetFileName(directory)));
                }
            }

            foreach (var moveMap in inputOutputMap)
            {
                if (!File.Exists(moveMap.Value))
                    File.Copy(moveMap.Key, moveMap.Value);
            }
        }
    }
}
