﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.ToolsFolder
{
    internal interface IBucket
    {
        void GeneratePoints(WriteableBitmap bitmap, System.Drawing.Point point, Color color, List<System.Drawing.Point> undoPoints, List<Color> undoColors);
    }
}
