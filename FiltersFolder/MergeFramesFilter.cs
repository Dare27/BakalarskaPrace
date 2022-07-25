using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.FiltersFolder
{
    internal class MergeFramesFilter : ToolSettings, ITwoFrameFilter
    {
        public WriteableBitmap GenerateFilter(WriteableBitmap currentBitmap, WriteableBitmap nextBitmap, int width, int height)
        {
            WriteableBitmap newBitmap = BitmapFactory.New(width, height);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    //Získání barvy pixelu z obou bitmap 
                    Color color01 = currentBitmap.GetPixel(i, j);
                    Color color02 = nextBitmap.GetPixel(i, j);

                    //Smíchání barev a zapsání barvy do nové bitmapy
                    Color finalColor = AlphaBlending(true, color02, color01);
                    newBitmap.SetPixel(i, j, finalColor);
                }
            }

            return newBitmap;
        }
    }
}
