using System;
using System.Collections.Generic;
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
        public void RotateImage(WriteableBitmap newBitmap, WriteableBitmap sourceBitmap)
        {
            int widthShift = 0;
            int heightShift = 0;
            CroppedBitmap croppedBitmap;

            //Zvolení posunu zkrácené bitmapy 
            if (sourceBitmap.PixelWidth < sourceBitmap.PixelHeight)
            {
                croppedBitmap = new CroppedBitmap(sourceBitmap, new Int32Rect(0, sourceBitmap.PixelHeight / 2 - sourceBitmap.PixelWidth / 2, sourceBitmap.PixelWidth, sourceBitmap.PixelHeight / 2 + sourceBitmap.PixelWidth / 2));
                heightShift = (sourceBitmap.PixelHeight / 2 - sourceBitmap.PixelWidth / 2);
            }
            else if (sourceBitmap.PixelHeight < sourceBitmap.PixelWidth)
            {
                croppedBitmap = new CroppedBitmap(sourceBitmap, new Int32Rect(sourceBitmap.PixelWidth / 2 - sourceBitmap.PixelHeight / 2, 0, sourceBitmap.PixelWidth / 2 + sourceBitmap.PixelHeight / 2, sourceBitmap.PixelHeight));
                widthShift = (sourceBitmap.PixelWidth / 2 - sourceBitmap.PixelHeight / 2);
            }
            else
            {
                croppedBitmap = new CroppedBitmap(sourceBitmap, new Int32Rect(0, 0, sourceBitmap.PixelWidth, sourceBitmap.PixelHeight));
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

        public void CenterAlligment(List<WriteableBitmap> bitmaps)
        {
            foreach (WriteableBitmap bitmap in bitmaps)
            {
                int leftPixelX = bitmap.PixelWidth;
                int rightPixelX = 0;
                int topPixelY = bitmap.PixelHeight;
                int downPixelY = 0;

                int currentLeftPixelX = bitmap.PixelWidth;
                int currentRightPixelX = 0;
                int currentTopPixelY = bitmap.PixelHeight;
                int currentDownPixelY = 0;

                //Projít dolu a doprava 
                for (int i = 0; i < bitmap.PixelWidth; i++)
                {
                    for (int j = 0; j < bitmap.PixelHeight; j++)
                    {
                        Color color = imageManipulation.GetPixelColor(i, j, bitmap);
                        if (color.A != 0)
                        {
                            if (currentRightPixelX < i)
                            {
                                currentRightPixelX = i;
                            }

                            if (currentDownPixelY < j)
                            {
                                currentDownPixelY = j;
                            }
                        }
                    }
                }

                //Projít nahoru a doleva
                for (int i = bitmap.PixelWidth; i >= 0; i--)
                {
                    for (int j = bitmap.PixelHeight; j >= 0; j--)
                    {
                        Color color = imageManipulation.GetPixelColor(i, j, bitmap);
                        if (color.A != 0)
                        {
                            if (currentLeftPixelX > i)
                            {
                                currentLeftPixelX = i;
                            }

                            if (currentTopPixelY > j)
                            {
                                currentTopPixelY = j;
                            }
                        }
                    }
                }

                //Zvolit maxima
                if (currentTopPixelY < topPixelY)
                {
                    topPixelY = currentTopPixelY;
                }

                if (currentLeftPixelX < leftPixelX)
                {
                    leftPixelX = currentLeftPixelX;
                }

                if (currentRightPixelX > rightPixelX)
                {
                    rightPixelX = currentRightPixelX;
                }

                if (currentDownPixelY > downPixelY)
                {
                    downPixelY = currentDownPixelY;
                }

                int croppedWidth = rightPixelX - leftPixelX + 1;
                int croppedHeight = downPixelY - topPixelY + 1;

                int startPosX = (bitmap.PixelWidth / 2) - (croppedWidth / 2);
                int startPosY = (bitmap.PixelHeight / 2) - (croppedHeight / 2);

                if (croppedWidth > 0 && croppedHeight > 0)
                {
                    Int32Rect rect = new Int32Rect(leftPixelX, topPixelY, croppedWidth, croppedHeight);

                    CroppedBitmap croppedBitmap = new CroppedBitmap(bitmap, rect);
                    WriteableBitmap newBitmap = new WriteableBitmap(croppedBitmap);
                    bitmap.Clear();

                    //Zapsání pixelů z staré bitmapy do nové
                    for (int i = 0; i < croppedWidth; i++)
                    {
                        for (int j = 0; j < croppedHeight; j++)
                        {
                            Color color = imageManipulation.GetPixelColor(i, j, newBitmap);
                            imageManipulation.AddPixel(i + startPosX, j + startPosY, color, bitmap);
                        }
                    }
                }
            }
        }
    }
}
