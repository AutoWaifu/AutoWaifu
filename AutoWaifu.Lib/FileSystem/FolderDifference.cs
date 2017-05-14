using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoWaifu.Lib.FileSystem
{
    public struct FolderDifference
    {
        public enum DifferenceType
        {
            FolderMissing,
            FileMissing,
            FolderAdded,
            FileAdded
        }


        public DifferenceType Type;
        public string AbsolutePath;
    }
}
