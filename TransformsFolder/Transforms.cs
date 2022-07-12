using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace.TransformsFolder
{
    internal class Transforms
    {
        FlipTransform flip = new FlipTransform();
        RotateTransform rotate = new RotateTransform();
        ResizeTransform resize = new ResizeTransform();
        CenterAlligmentTransform center = new CenterAlligmentTransform();
        CropToFitTransform cropToFit = new CropToFitTransform();

        public void Flip (List<int> selectedBitmapIndexes, List<WriteableBitmap> bitmaps, bool alternativeFunction)
        {
            flip.GenerateTransform(selectedBitmapIndexes, bitmaps, alternativeFunction);
        }

        public void Rotate(List<int> selectedBitmapIndexes, List<WriteableBitmap> bitmaps, bool alternativeFunction)
        {
            rotate.GenerateTransform(selectedBitmapIndexes, bitmaps, alternativeFunction);
        }

        public void Resize(List<List<WriteableBitmap>> layers, int newWidth, int newHeight, string position)
        {
            resize.GenerateTransform(layers, newWidth, newHeight, position);
        }

        public void CenterAlligment(List<int> currentBitmapIndexes, List<WriteableBitmap> bitmaps) 
        { 
            center.GenerateTransform(currentBitmapIndexes, bitmaps);
        }

        public void CropToFit(List<List<WriteableBitmap>> layers) 
        {
            cropToFit.GenerateTransform(layers);
        }
    }
}
