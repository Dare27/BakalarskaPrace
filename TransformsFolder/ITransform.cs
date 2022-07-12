using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.TransformsFolder
{
    internal interface ITransform
    {
        void GenerateTransform(List<int> selectedBitmapIndexes, List<WriteableBitmap> bitmaps, bool alternativeFunction);
    }
}
