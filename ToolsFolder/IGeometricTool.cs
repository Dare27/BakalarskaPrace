using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal interface IGeometricTool
    {
        List<System.Drawing.Point> GeneratePoints(WriteableBitmap bitmap, System.Drawing.Point startPoint, System.Drawing.Point endPoint, bool alternativeFunction01, bool alternativeFunction02) ;
    }
}
