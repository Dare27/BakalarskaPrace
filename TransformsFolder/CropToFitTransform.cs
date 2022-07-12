using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        Color color = bitmap.GetPixel(i, j);
                        if (color.A != 0)
                        {
                            if (currentRightPixelX < i) currentRightPixelX = i;
                            if (currentDownPixelY < j) currentDownPixelY = j;
                        }
                    }
                }

                //Projít nahoru a doleva
                for (int i = width; i >= 0; i--)
                {
                    for (int j = height; j >= 0; j--)
                    {
                        Color color = bitmap.GetPixel(i, j);
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
            }

            int newWidth = rightPixelX - leftPixelX + 1;
            int newHeight = downPixelY - topPixelY + 1;
            if (newWidth > 0 && newHeight > 0)
            {
                for (int k = 0; k < layers.Count; k++)
                {
                    for (int l = 0; l < layers[k].Count; l++)
                    {
                        WriteableBitmap newBitmap = BitmapFactory.New(newWidth, newHeight);
                        //Získání pixelů z aktuální bitmapy
                        using (newBitmap.GetBitmapContext())
                        {
                            for (int i = leftPixelX; i <= rightPixelX; i++)
                            {
                                for (int j = topPixelY; j <= downPixelY; j++)
                                {
                                    Color color = layers[k][l].GetPixel(i, j);
                                    if (color.A != 0)
                                    {
                                        //Vytvoření pixelu, který je posunutý v nové bitmapě 
                                        newBitmap.SetPixel(i - leftPixelX, j - topPixelY, color);
                                    }
                                }
                            }
                        }
                        layers[k][l] = newBitmap;
                    }
                }
            }
        }
    }
}
