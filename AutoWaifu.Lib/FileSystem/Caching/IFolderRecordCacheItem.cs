using AutoWaifu.Lib.Waifu2x;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.FileSystem
{
    public abstract class IFolderRecordCacheItem
    {
        public IFolderRecordCacheItem()
        {

        }

        public IFolderRecordCacheItem(string filePath)
        {
            FilePath = filePath;
        }

        public string FilePath { get; }
        public DateTime UtcDateCacheItemCreated { get; }


        public bool IsOld
        {
            get
            {
                if (FilePath == null)
                    return true;

                var fileModifiedDate = File.GetLastWriteTimeUtc(FilePath);
                return fileModifiedDate != UtcDateCacheItemCreated;
            }
        }

        public abstract ImageResolution ImageResolution { get; }
        public abstract int NumFrames { get; }
    }
}
