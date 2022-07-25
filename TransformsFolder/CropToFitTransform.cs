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
    internal class CropToFitTransform
    {
        public void GenerateTransform(List<List<WriteableBitmap>> layers)
        {
            List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();
            for (int j = 0; j < layers.Count; j++)
            {
                for (int i = 0; i < layers[j].Count; i++)
                {
                    bitmaps.Add(layers[j][i]);
                }
            }

            int width = bitmaps[0].PixelWidth;
            int height = bitmaps[0].PixelHeight;
            int leftPixelX = width;
            int rightPixelX = 0;
            int topPixelY = height;
            int downPixelY = 0;

            foreach (WriteableBitmap bitmap in bitmaps)
            {
                int currentLeftPixelX = width;
                int currentRightPixelX = 0;
                int currentTopPixelY = height;
                int currentDownPixelY = 0;

                //Projít dolu a doprava 
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Color color = bitmap.GetPixel(x, y);
                        if (color.A != 0)
                        {
                            if (currentRightPixelX < x) currentRightPixelX = x;
                            if (currentDownPixelY < y) currentDownPixelY = y;
                        }
                    }
                }

                //Projít nahoru a doleva
                for (int x = width; x >= 0; x--)
                {
                    for (int y = height; y >= 0; y--)
                    {
                        Color color = bitmap.GetPixel(x, y);
                        if (color.A != 0)
                        {
                            if (currentLeftPixelX > x) currentLeftPixelX = x;
                            if (currentTopPixelY > y) currentTopPixelY = y;
                        }
                    }
                }

                //Zvolit maxima
                if (currentTopPixelY < topPixelY) topPixelY = currentTopPixelY;
                if (currentLeftPixelX < leftPixelX) leftPixelX = currentLeftPixelX;
                if (currentRightPixelX > rightPixelX) rightPixelX = currentRightPixelX;
                if (currentDownPixelY > downPixelY) downPixelY = currentDownPixelY;
            }

            int croppedWidth = rightPixelX - leftPixelX + 1;
            int croppedHeight = downPixelY - topPixelY + 1;
            if (croppedWidth > 0 && croppedHeight > 0)
            {
                for (int k = 0; k < layers.Count; k++)
                {
                    for (int l = 0; l < layers[k].Count; l++)
                    {
                        Int32Rect rect = new Int32Rect(leftPixelX, topPixelY, croppedWidth, croppedHeight);
                        CroppedBitmap croppedBitmap = new CroppedBitmap(layers[k][l], rect);
                        WriteableBitmap newBitmap = new WriteableBitmap(croppedBitmap);
                        layers[k][l] = newBitmap;
                    }
                }
            }
        }
    }
}
