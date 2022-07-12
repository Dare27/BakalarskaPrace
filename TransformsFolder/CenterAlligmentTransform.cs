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
                int leftPixelX = bitmaps[0].PixelWidth;
                int rightPixelX = 0;
                int topPixelY = bitmaps[0].PixelHeight;
                int downPixelY = 0;

                int currentLeftPixelX = bitmaps[0].PixelWidth;
                int currentRightPixelX = 0;
                int currentTopPixelY = bitmaps[0].PixelHeight;
                int currentDownPixelY = 0;

                //Projít dolu a doprava 
                for (int i = 0; i < bitmaps[0].PixelWidth; i++)
                {
                    for (int j = 0; j < bitmaps[0].PixelHeight; j++)
                    {
                        Color color = bitmaps[k].GetPixel(i, j);
                        if (color.A != 0)
                        {
                            if (currentRightPixelX < i) currentRightPixelX = i;
                            if (currentDownPixelY < j) currentDownPixelY = j;
                        }
                    }
                }

                //Projít nahoru a doleva
                for (int i = bitmaps[0].PixelWidth; i >= 0; i--)
                {
                    for (int j = bitmaps[0].PixelHeight; j >= 0; j--)
                    {
                        Color color = bitmaps[k].GetPixel(i, j);
                        if (color.A != 0)
                        {
                            if (currentLeftPixelX > i) currentLeftPixelX = i;
                            if (currentTopPixelY > j) currentTopPixelY = j;
                        }
                    }
                }

                //Zvolit maxima
                if (currentTopPixelY < topPixelY) topPixelY = currentTopPixelY;
                if (currentLeftPixelX < leftPixelX) leftPixelX = currentLeftPixelX;
                if (currentRightPixelX > rightPixelX) rightPixelX = currentRightPixelX;
                if (currentDownPixelY > downPixelY) downPixelY = currentDownPixelY;

                int croppedWidth = rightPixelX - leftPixelX + 1;
                int croppedHeight = downPixelY - topPixelY + 1;

                int startPosX = (bitmaps[0].PixelWidth / 2) - (croppedWidth / 2);
                int startPosY = (bitmaps[0].PixelHeight / 2) - (croppedHeight / 2);

                if (croppedWidth > 0 && croppedHeight > 0)
                {
                    Int32Rect rect = new Int32Rect(leftPixelX, topPixelY, croppedWidth, croppedHeight);

                    CroppedBitmap croppedBitmap = new CroppedBitmap(bitmaps[k], rect);
                    WriteableBitmap newBitmap = new WriteableBitmap(croppedBitmap);
                    WriteableBitmap finalBitmap = BitmapFactory.New(bitmaps[0].PixelWidth, bitmaps[0].PixelHeight);

                    //Zapsání pixelů z staré bitmapy do nové
                    for (int i = 0; i < croppedWidth; i++)
                    {
                        for (int j = 0; j < croppedHeight; j++)
                        {
                            Color color = newBitmap.GetPixel(i, j);
                            finalBitmap.SetPixel(i + startPosX, j + startPosY, color);
                        }
                    }
                    bitmaps[k] = finalBitmap;
                }
            }
        }
    }
}
