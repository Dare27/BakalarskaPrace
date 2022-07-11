using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal interface IBucket
    {
        List<System.Drawing.Point> GeneratePoints(WriteableBitmap bitmap, System.Drawing.Point startPoint);
    }
}
