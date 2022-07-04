using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace
{
    internal class Filters
    {
        public WriteableBitmap IntersectImages(WriteableBitmap currentBitmap, WriteableBitmap nextBitmap, int width, int height)
        {
            WriteableBitmap newBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
            ImageManipulation imageManipulation = new ImageManipulation();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    //Získání barvy pixelu z obou bitmap 
                    Color color01 = imageManipulation.GetPixelColor(i, j, currentBitmap);
                    Color color02 = imageManipulation.GetPixelColor(i, j, nextBitmap);
                    Color finalColor;

                    //Smíchání barev a zapsání barvy do nové bitmapy
                    if (color01.A != 0 && color02.A != 0)
                    {
                        finalColor = imageManipulation.ColorMix(color02, color01);
                    }
                    else
                    {
                        finalColor = Color.FromArgb(0, 0, 0, 0);
                    }

                    imageManipulation.AddPixel(i, j, finalColor, newBitmap);
                }
            }

            return newBitmap;
        }

        public WriteableBitmap MergeImages(WriteableBitmap currentBitmap, WriteableBitmap nextBitmap, int width, int height)
        {
            WriteableBitmap newBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
            ImageManipulation imageManipulation = new ImageManipulation();
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    //Získání barvy pixelu z obou bitmap 
                    Color color01 = imageManipulation.GetPixelColor(i, j, currentBitmap);
                    Color color02 = imageManipulation.GetPixelColor(i, j, nextBitmap);

                    //Smíchání barev a zapsání barvy do nové bitmapy
                    Color finalColor = imageManipulation.ColorMix(color02, color01);
                    imageManipulation.AddPixel(i, j, finalColor, newBitmap);
                }
            }

            return newBitmap;
        }

        public WriteableBitmap MergeAllLayers(List<List<WriteableBitmap>> layers, int index, int width, int height) 
        {
            WriteableBitmap combinedBitmap = BitmapFactory.New(width, height);

            for (int i = 0; i < layers.Count - 1; i++)
            {
                combinedBitmap = MergeImages(layers[i][index], layers[i + 1][index], width, height);
            }

            return combinedBitmap;
        }
    }
}
