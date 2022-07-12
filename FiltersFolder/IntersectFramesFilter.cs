using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.FiltersFolder
{
    internal class IntersectFramesFilter : ToolSettings, ITwoFrameFilter
    {
        public WriteableBitmap GenerateFilter(WriteableBitmap currentBitmap, WriteableBitmap nextBitmap, int width, int height)
        {
            WriteableBitmap newBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    //Získání barvy pixelu z obou bitmap 
                    Color color01 = currentBitmap.GetPixel(i, j);
                    Color color02 = nextBitmap.GetPixel(i, j);
                    Color finalColor;

                    //Smíchání barev a zapsání barvy do nové bitmapy
                    if (color01.A != 0 && color02.A != 0)
                    {
                        finalColor = AlphaBlending(true, color02, color01);
                    }
                    else
                    {
                        finalColor = Color.FromArgb(0, 0, 0, 0);
                    }

                    newBitmap.SetPixel(i, j, finalColor);
                }
            }

            return newBitmap;
        }
    }
}
