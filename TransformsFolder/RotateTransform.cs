using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.TransformsFolder
{
    internal class RotateTransform : ITransform
    {
        //Může vést ke ztrátě obsahu, pokud není šířka rovná výšce
        //AlternativeFunction je rotace o 90 stupňů
        public void GenerateTransform(List<int> selectedBitmapIndexes, List<WriteableBitmap> bitmaps, bool alternativeFunction)
        {
            int width = bitmaps[0].PixelWidth;
            int height = bitmaps[0].PixelHeight;
            int widthShift = 0;
            int heightShift = 0;

            foreach (int i in selectedBitmapIndexes)
            {
                CroppedBitmap croppedBitmap;

                //Zvolení posunu zkrácené bitmapy 
                if (width < height)
                {
                    croppedBitmap = new CroppedBitmap(bitmaps[i], new Int32Rect(0, height / 2 - width / 2, width, height / 2 + width / 2));
                    heightShift = (height / 2 - width / 2);
                }
                else if (height < width)
                {
                    croppedBitmap = new CroppedBitmap(bitmaps[i], new Int32Rect(width / 2 - height / 2, 0, width / 2 + height / 2, height));
                    widthShift = (width / 2 - height / 2);
                }
                else
                {
                    croppedBitmap = new CroppedBitmap(bitmaps[i], new Int32Rect(0, 0, width, height));
                }

                WriteableBitmap temporaryBitmap = new WriteableBitmap(croppedBitmap);
                int size = Math.Min(temporaryBitmap.PixelWidth, temporaryBitmap.PixelHeight);
                WriteableBitmap rotatedBitmap = new WriteableBitmap(size, size, 1, 1, PixelFormats.Bgra32, null);

                //Rotace dočasné bitmapy
                for (int x = 0; x < rotatedBitmap.PixelWidth; x++)
                {
                    for (int y = 0; y < rotatedBitmap.PixelHeight; y++)
                    {
                        Color color = temporaryBitmap.GetPixel(x, y);
                        //Směr rotace
                        if (alternativeFunction == true) rotatedBitmap.SetPixel(rotatedBitmap.PixelHeight - y - 1, x, color);
                        else rotatedBitmap.SetPixel(y, rotatedBitmap.PixelWidth - x - 1, color);
                    }
                }

                //Zapsaní otočené bitmapy s případným posunem do bitmapy
                for (int x = 0; x < rotatedBitmap.PixelWidth; x++)
                {
                    for (int y = 0; y < rotatedBitmap.PixelHeight; y++)
                    {
                        Color color = rotatedBitmap.GetPixel(x, y);
                        bitmaps[i].SetPixel(x + widthShift, y + heightShift, color);
                    }
                }
            }
        }
    }
}
