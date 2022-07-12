using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.FiltersFolder
{
    internal class Filters: ToolSettings
    {
        MergeFramesFilter mergeFrames = new MergeFramesFilter();
        IntersectFramesFilter intersectFrames = new IntersectFramesFilter();

        public WriteableBitmap MergeFrames(WriteableBitmap currentBitmap, WriteableBitmap nextBitmap, int width, int height) 
        {
            return mergeFrames.GenerateFilter(currentBitmap, nextBitmap, width, height);
        }

        public WriteableBitmap IntersectFrames(WriteableBitmap currentBitmap, WriteableBitmap nextBitmap, int width, int height)
        {
            return intersectFrames.GenerateFilter(currentBitmap, nextBitmap, width, height);
        }

        public WriteableBitmap MergeAllLayers(List<List<WriteableBitmap>> layers, int index, int width, int height) 
        {
            WriteableBitmap combinedBitmap = BitmapFactory.New(width, height);

            if (layers[0].Count != 1)
            {
                for (int i = 0; i < layers.Count - 1; i++)
                {
                    if (i + 1 < layers.Count - 1)
                    {
                        combinedBitmap = MergeFrames(layers[i][index], layers[i + 1][index], width, height);
                    }
                }
            }
            else 
            {
                combinedBitmap = layers[0][index];
            }
            

            return combinedBitmap;
        }

        public WriteableBitmap CreateCompositeBitmap(List<List<WriteableBitmap>> layers)
        {
            int width = layers[0][0].PixelWidth;
            int height = layers[0][0].PixelHeight;
            int finalWidth = width;
            int finalHeight = height * layers[0].Count;
            WriteableBitmap finalBitmap = BitmapFactory.New(finalWidth, finalHeight);
            List<WriteableBitmap> combinedBitmaps = new List<WriteableBitmap>();

            for (int i = 0; i < layers[0].Count; i++) 
            {
                WriteableBitmap combinedBitmap = MergeAllLayers(layers, i, width, height);
                combinedBitmaps.Add(combinedBitmap);
            }

            using (finalBitmap.GetBitmapContext())
            {
                for (int k = 0; k < combinedBitmaps.Count; k++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            //Color color = layers[0][k].GetPixel(i, j);
                            Color color = combinedBitmaps[k].GetPixel(i, j);
                            finalBitmap.SetPixel(i, j + (k * height), color);
                        }
                    }
                }
            }
            return finalBitmap;
        }
    }
}
