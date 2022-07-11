using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace
{
    internal class Filters: ToolSettings
    {
        public WriteableBitmap IntersectImages(WriteableBitmap currentBitmap, WriteableBitmap nextBitmap, int width, int height)
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

        public WriteableBitmap MergeImages(WriteableBitmap currentBitmap, WriteableBitmap nextBitmap, int width, int height)
        {
            WriteableBitmap newBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
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

        public WriteableBitmap MergeAllLayers(List<List<WriteableBitmap>> layers, int index, int width, int height) 
        {
            WriteableBitmap combinedBitmap = BitmapFactory.New(width, height);
            
            for (int i = 0; i < layers.Count - 1; i++)
            {
                if (i + 1  < layers.Count - 1)
                {
                    combinedBitmap = MergeImages(layers[i][index], layers[i + 1][index], width, height);
                }
            }

            return combinedBitmap;
        }
    }
}
