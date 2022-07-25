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
    internal class CenterAlligmentTransform
    {
        public void GenerateTransform(List<int> currentBitmapIndexes, List<WriteableBitmap> bitmaps)
        {
            foreach (int k in currentBitmapIndexes)
            {
                int leftPixel = bitmaps[0].PixelWidth;
                int rightPixel = 0;
                int topPixel = bitmaps[0].PixelHeight;
                int downPixel = 0;

                //Projít dolu a doprava 
                for (int x = 0; x < bitmaps[0].PixelWidth; x++)
                {
                    for (int y = 0; y < bitmaps[0].PixelHeight; y++)
                    {
                        Color color = bitmaps[k].GetPixel(x, y);
                        if (color.A != 0)
                        {
                            if (x > rightPixel) rightPixel = x;
                            if (y > downPixel) downPixel = y;
                        }
                    }
                }

                //Projít nahoru a doleva
                for (int x = bitmaps[0].PixelWidth; x >= 0; x--)
                {
                    for (int y = bitmaps[0].PixelHeight; y >= 0; y--)
                    {
                        Color color = bitmaps[k].GetPixel(x, y);
                        if (color.A != 0)
                        {
                            if (x < leftPixel) leftPixel = x;
                            if (y < topPixel) topPixel = y;
                        }
                    }
                }

                int croppedWidth = rightPixel - leftPixel + 1;
                int croppedHeight = downPixel - topPixel + 1;

                if (croppedWidth > 0 && croppedHeight > 0)
                {
                    Int32Rect rect = new Int32Rect(leftPixel, topPixel, croppedWidth, croppedHeight);
                    CroppedBitmap croppedBitmap = new CroppedBitmap(bitmaps[k], rect);
                    WriteableBitmap newBitmap = new WriteableBitmap(croppedBitmap);
                    bitmaps[k].Clear();
                    int startPosX = (bitmaps[0].PixelWidth / 2) - (croppedWidth / 2);
                    int startPosY = (bitmaps[0].PixelHeight / 2) - (croppedHeight / 2);
                    using (bitmaps[k].GetBitmapContext())
                    {
                        //Zapsání pixelů z staré bitmapy do nové
                        for (int x = 0; x < croppedWidth; x++)
                        {
                            for (int y = 0; y < croppedHeight; y++)
                            {
                                Color color = newBitmap.GetPixel(x, y);
                                bitmaps[k].SetPixel(x + startPosX, y + startPosY, color);
                            }
                        }
                    }
                }
            }
        }
    }
}
