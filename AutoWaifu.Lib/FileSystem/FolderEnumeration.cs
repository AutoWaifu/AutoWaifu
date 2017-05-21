using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.FileSystem
{
    public class FolderEnumeration
    {
        FileSystemWatcher watcher;

        public FolderEnumeration(string folderPath)
        {
            FolderPath = folderPath;

            watcher = new FileSystemWatcher(folderPath);
            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = true;
            watcher.NotifyFilter =  NotifyFilters.FileName |
                                    NotifyFilters.DirectoryName |
                                    NotifyFilters.Attributes |
                                    NotifyFilters.CreationTime |
                                    NotifyFilters.LastAccess |
                                    NotifyFilters.LastWrite |
                                    NotifyFilters.Security |
                                    NotifyFilters.Size;

            watcher.Renamed += Watcher_Renamed;
            watcher.Created += Watcher_Created;
            watcher.Deleted += Watcher_Deleted;
        }

        /// <summary>
        /// Format of ie "gifv|mp4|webm"
        /// </summary>
        public string Filter { get; set; }

        bool PassesFilter(string file)
        {
            if (string.IsNullOrEmpty(Filter))
                return true;

            file = file.Trim('.');
            string[] allowedExtensions = Filter.Split('|');

            string fileExtension = Path.GetExtension(file);
            return allowedExtensions.Any(ext => $".{ext.Trim('.')}".ToLower() == fileExtension.ToLower());
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            var ext = Path.GetExtension(e.FullPath);
            if (ext.Length == 0)
                FolderRenamed?.Invoke(e.FullPath);
            else if (PassesFilter(e.FullPath))
                FileRenamed?.Invoke(e.FullPath);
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            string ext = Path.GetExtension(e.FullPath);
            if (ext.Length == 0)
                FolderRemoved?.Invoke(e.FullPath);
            else if (PassesFilter(e.FullPath))
                FileRemoved?.Invoke(e.FullPath);
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            string ext = Path.GetExtension(e.FullPath);

            if (ext.Length == 0)
                FolderAdded?.Invoke(e.FullPath);
            else if (PassesFilter(e.FullPath))
                FileAdded?.Invoke(e.FullPath);
        }

        public string FolderPath { get; }

        public event Action<string> FileAdded;
        public event Action<string> FileRemoved;

        public event Action<string> FolderAdded;
        public event Action<string> FolderRemoved;

        public event Action<string> FileRenamed;
        public event Action<string> FolderRenamed;


        public IEnumerable<string> FilePaths => Directory.EnumerateFiles(FolderPath, "*", SearchOption.AllDirectories).Where(p => PassesFilter(p));
        public IEnumerable<string> FolderPaths => Directory.EnumerateDirectories(FolderPath, "*", SearchOption.AllDirectories);

        public IEnumerable<string> RelativeFilePaths => FilePaths.Select(p => p.Replace(FolderPath, string.Empty).Trim('/', '\\'));
        public IEnumerable<string> RelativeFolderPaths => FolderPaths.Select(p => p.Replace(FolderPath, string.Empty).Trim('/', '\\'));



        /// <summary>
        /// Returns the list of added files in other, removed files in other, etc. compared to this.
        /// </summary>
        /// <returns>A set of FolderDifferences. A folder difference of FileAdded means that Other has an added file compared to This, and vice-versa.</returns>
        public IEnumerable<FolderDifference> DiffAgainst(FolderEnumeration other)
        {
            var thisRoot = this.FolderPath;
            var otherRoot = other.FolderPath;

            var thisFolderPaths = RelativeFolderPaths.ToArray();
            var otherFolderPaths = other.RelativeFolderPaths.ToArray();

            //  TODO - Use hash map to avoid N^2 iteration via .Contains()
            var thisFilePaths = RelativeFilePaths.ToArray();
            var otherFilePaths = other.RelativeFilePaths.ToArray();

            var missingFoldersInOther = thisFolderPaths.Where(tp => !otherFolderPaths.Contains(tp)).ToArray();
            var newFoldersInOther = otherFolderPaths.Where(op => !thisFolderPaths.Contains(op)).ToArray();

            var missingFilesInOther = thisFilePaths.Where(tp => !otherFilePaths.Contains(tp)).ToArray();
            var newFilesInOther = otherFilePaths.Where(op => !thisFilePaths.Contains(op)).ToArray();

            string FullOtherPath(string inputPath)
            {
                return Path.GetFullPath(Path.Combine(other.FolderPath, inputPath));
            }

            foreach (var missingFolder in missingFoldersInOther)
                yield return new FolderDifference { AbsolutePath = FullOtherPath(missingFolder), Type = FolderDifference.DifferenceType.FolderMissing };

            foreach (var newFolder in newFoldersInOther)
                yield return new FolderDifference { AbsolutePath = FullOtherPath(newFolder), Type = FolderDifference.DifferenceType.FolderAdded };

            foreach (var missingFile in missingFilesInOther)
                yield return new FolderDifference { AbsolutePath = FullOtherPath(missingFile), Type = FolderDifference.DifferenceType.FileMissing };

            foreach (var newFile in newFilesInOther)
                yield return new FolderDifference { AbsolutePath = FullOtherPath(newFile), Type = FolderDifference.DifferenceType.FileAdded };
        }

    }
}
