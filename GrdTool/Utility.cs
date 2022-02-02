﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrdTool
{
    static class Utility
    {
        public static bool PathIsFolder(string path)
        {
            return new FileInfo(path).Attributes.HasFlag(FileAttributes.Directory);
        }
    }
}
