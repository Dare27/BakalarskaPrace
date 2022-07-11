using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BakalarskaPrace.ToolsFolder
{
    internal interface IGeometricTool
    {
        List<System.Drawing.Point> GeneratePoints(System.Drawing.Point startPoint, System.Drawing.Point endPoint, bool alternativeFunction01, bool alternativeFunction02, int width, int height) ;
    }
}
