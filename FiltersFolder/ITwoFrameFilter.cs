using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using BakalarskaPrace.ToolsFolder;

namespace BakalarskaPrace.FiltersFolder
{
    internal interface ITwoFrameFilter
    {
        WriteableBitmap GenerateFilter(WriteableBitmap currentBitmap, WriteableBitmap nextBitmap, int width, int height);
    }
}
