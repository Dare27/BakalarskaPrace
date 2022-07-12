using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal class DefaultBrush : IBrush
    {
        public List<System.Drawing.Point> GeneratePoints(WriteableBitmap bitmap, System.Drawing.Point point) 
        {
            return new List<System.Drawing.Point> { point };
        }
    }
}
