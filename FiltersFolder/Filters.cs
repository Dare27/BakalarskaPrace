using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.FiltersFolder
{
    internal class Filters
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

            if (layers.Count != 1)
            {
                for (int i = 0; i < layers.Count - 1; i++)
                {
                    combinedBitmap = MergeFrames(layers[i][index], layers[i + 1][index], width, height);
                }
            }
            else
            {
                combinedBitmap = layers[0][index];
            }

            return combinedBitmap;
        }
    }
}
