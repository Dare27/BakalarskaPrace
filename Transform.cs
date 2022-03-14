using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace
{
    internal class Transform
    {
        ImageManipulation imageManipulation;

        public Transform()
        {
            imageManipulation = new ImageManipulation();
        }

        public void Flip(WriteableBitmap newBitmap, WriteableBitmap sourceBitmap)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                for (int x = 0; x < sourceBitmap.PixelWidth; x++)
                {
                    for (int y = 0; y < sourceBitmap.PixelHeight; y++)
                    {
                        int yp = sourceBitmap.PixelHeight - y - 1;
                        Color color = imageManipulation.GetPixelColor(x, y, sourceBitmap);
                        imageManipulation.AddPixel(x, yp, color, newBitmap);
                    }
                }
            }
            else
            {
                for (int x = 0; x < sourceBitmap.PixelWidth; x++)
                {
                    for (int y = 0; y < sourceBitmap.PixelHeight; y++)
                    {
                        int xp = sourceBitmap.PixelWidth - x - 1;
                        Color color = imageManipulation.GetPixelColor(x, y, sourceBitmap);
                        imageManipulation.AddPixel(xp, y, color, newBitmap);
                    }
                }
            }
        }

        public void RotateAnimation(WriteableBitmap newBitmap, WriteableBitmap sourceBitmap)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                for (int x = 0; x < sourceBitmap.PixelWidth; x++)
                {
                    for (int y = 0; y < sourceBitmap.PixelHeight; y++)
                    {
                        Color color = imageManipulation.GetPixelColor(x, y, sourceBitmap);
                        imageManipulation.AddPixel(sourceBitmap.PixelHeight - y - 1, x, color, newBitmap);
                    }
                }
            }
            else
            {
                for (int x = 0; x < sourceBitmap.PixelWidth; x++)
                {
                    for (int y = 0; y < sourceBitmap.PixelHeight; y++)
                    {
                        Color color = imageManipulation.GetPixelColor(x, y, sourceBitmap);
                        imageManipulation.AddPixel(y, sourceBitmap.PixelWidth - x - 1, color, newBitmap);
                    }
                }
            }
        }

        //Může vést ke ztrátě obsahu, pokud není šířka rovná výšce
        public void RotateImage(WriteableBitmap newBitmap, WriteableBitmap sourceBitmap, int width, int height)
        {
            int widthShift = 0;
            int heightShift = 0;
            CroppedBitmap croppedBitmap;

            //Zvolení posunu zkrácené bitmapy 
            if (width < height)
            {
                croppedBitmap = new CroppedBitmap(sourceBitmap, new Int32Rect(0, height / 2 - width / 2, width, height / 2 + width / 2));
                heightShift = (height / 2 - width / 2);
            }
            else if (height < width)
            {
                croppedBitmap = new CroppedBitmap(sourceBitmap, new Int32Rect(width / 2 - height / 2, 0, width / 2 + height / 2, height));
                widthShift = (width / 2 - height / 2);
            }
            else
            {
                croppedBitmap = new CroppedBitmap(sourceBitmap, new Int32Rect(0, 0, width, height));
            }

            WriteableBitmap temporaryBitmap = new WriteableBitmap(croppedBitmap);
            int size = Math.Min(temporaryBitmap.PixelWidth, temporaryBitmap.PixelHeight);
            WriteableBitmap rotatedBitmap = new WriteableBitmap(size, size, 1, 1, PixelFormats.Bgra32, null);

            //Rotace dočasné bitmapy
            for (int x = 0; x < rotatedBitmap.PixelWidth; x++)
            {
                for (int y = 0; y < rotatedBitmap.PixelHeight; y++)
                {
                    //Směr rotace
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        Color color = imageManipulation.GetPixelColor(x, y, temporaryBitmap);
                        imageManipulation.AddPixel(rotatedBitmap.PixelHeight - y - 1, x, color, rotatedBitmap);
                    }
                    else
                    {
                        Color color = imageManipulation.GetPixelColor(x, y, temporaryBitmap);
                        imageManipulation.AddPixel(y, rotatedBitmap.PixelWidth - x - 1, color, rotatedBitmap);
                    }
                }
            }

            //Zapsaní otočené bitmapy s případným posunem do nové bitmapy
            for (int x = 0; x < rotatedBitmap.PixelWidth; x++)
            {
                for (int y = 0; y < rotatedBitmap.PixelHeight; y++)
                {
                    Color color = imageManipulation.GetPixelColor(x, y, rotatedBitmap);
                    imageManipulation.AddPixel(x + widthShift, y + heightShift, color, newBitmap);
                }
            }
        }
    }
}
