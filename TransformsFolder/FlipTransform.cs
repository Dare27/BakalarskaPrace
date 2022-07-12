using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.TransformsFolder
{
    internal class FlipTransform : ITransform
    {
        public void GenerateTransform(List<int> selectedBitmapIndexes, List<WriteableBitmap> bitmaps, bool alternativeFunction)
        {
            int width = bitmaps[0].PixelWidth;
            int height = bitmaps[0].PixelHeight;

            foreach (int i in selectedBitmapIndexes)
            {
                WriteableBitmap newBitmap = BitmapFactory.New(width, height);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Color color = bitmaps[i].GetPixel(x, y);
                        if (alternativeFunction == false)
                        {
                            int yp = bitmaps[i].PixelHeight - y - 1;
                            newBitmap.SetPixel(x, yp, color);
                        }
                        else
                        {
                            int xp = bitmaps[i].PixelWidth - x - 1;
                            newBitmap.SetPixel(xp, y, color);
                        }
                    }
                }

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Color color = newBitmap.GetPixel(x, y);
                        bitmaps[i].SetPixel(x, y, color);
                    }
                }
            }
        }
    }
}
