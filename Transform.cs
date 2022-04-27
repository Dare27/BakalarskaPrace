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

        public void Flip(List<int> selectedBitmapIndexes, List<WriteableBitmap> bitmaps, bool horizontal)
        {
            int width = bitmaps[0].PixelWidth;
            int height = bitmaps[0].PixelHeight;

            foreach (int i in selectedBitmapIndexes)
            {
                WriteableBitmap newBitmap = BitmapFactory.New(width, height);
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Color color = imageManipulation.GetPixelColor(x, y, bitmaps[i]);
                        if (horizontal == false)
                        {
                            int yp = bitmaps[i].PixelHeight - y - 1;
                            imageManipulation.AddPixel(x, yp, color, newBitmap);
                        }
                        else 
                        {
                            int xp = bitmaps[i].PixelWidth - x - 1;
                            imageManipulation.AddPixel(xp, y, color, newBitmap);
                        }
                    }
                }

                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        Color color = imageManipulation.GetPixelColor(x, y, newBitmap);
                        imageManipulation.AddPixel(x, y, color, bitmaps[i]);
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
        public void RotateImage(List<int> currentBitmapIndexes, List<WriteableBitmap> bitmaps, bool clockwise)
        {
            int width = bitmaps[0].PixelWidth;
            int height = bitmaps[0].PixelHeight;
            int widthShift = 0;
            int heightShift = 0;

            foreach(int i in currentBitmapIndexes)
            {
                CroppedBitmap croppedBitmap;

                //Zvolení posunu zkrácené bitmapy 
                if (width < height)
                {
                    croppedBitmap = new CroppedBitmap(bitmaps[i], new Int32Rect(0, height / 2 - width / 2, width, height / 2 + width / 2));
                    heightShift = (height / 2 - width / 2);
                }
                else if (height < width)
                {
                    croppedBitmap = new CroppedBitmap(bitmaps[i], new Int32Rect(width / 2 - height / 2, 0, width / 2 + height / 2, height));
                    widthShift = (width / 2 - height / 2);
                }
                else
                {
                    croppedBitmap = new CroppedBitmap(bitmaps[i], new Int32Rect(0, 0, width, height));
                }

                WriteableBitmap temporaryBitmap = new WriteableBitmap(croppedBitmap);
                int size = Math.Min(temporaryBitmap.PixelWidth, temporaryBitmap.PixelHeight);
                WriteableBitmap rotatedBitmap = new WriteableBitmap(size, size, 1, 1, PixelFormats.Bgra32, null);

                //Rotace dočasné bitmapy
                for (int x = 0; x < rotatedBitmap.PixelWidth; x++)
                {
                    for (int y = 0; y < rotatedBitmap.PixelHeight; y++)
                    {
                        Color color = imageManipulation.GetPixelColor(x, y, temporaryBitmap);
                        //Směr rotace
                        if (clockwise == true)
                            imageManipulation.AddPixel(rotatedBitmap.PixelHeight - y - 1, x, color, rotatedBitmap);
                        else
                            imageManipulation.AddPixel(y, rotatedBitmap.PixelWidth - x - 1, color, rotatedBitmap);
                    }
                }

                //Zapsaní otočené bitmapy s případným posunem do bitmapy
                for (int x = 0; x < rotatedBitmap.PixelWidth; x++)
                {
                    for (int y = 0; y < rotatedBitmap.PixelHeight; y++)
                    {
                        Color color = imageManipulation.GetPixelColor(x, y, rotatedBitmap);
                        imageManipulation.AddPixel(x + widthShift, y + heightShift, color, bitmaps[i]);
                    }
                }
            }
        }

        public void CropToFit(List<WriteableBitmap> bitmaps)
        {
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
                        Color color = imageManipulation.GetPixelColor(i, j, bitmap);
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
                        Color color = imageManipulation.GetPixelColor(i, j, bitmap);
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
                for (int k = 0; k < bitmaps.Count; k++)
                {
                    WriteableBitmap newBitmap = BitmapFactory.New(newWidth, newHeight);
                    //Získání pixelů z aktuální bitmapy
                    using (newBitmap.GetBitmapContext())
                    {
                        for (int i = leftPixelX; i <= rightPixelX; i++)
                        {
                            for (int j = topPixelY; j <= downPixelY; j++)
                            {
                                Color color = imageManipulation.GetPixelColor(i, j, bitmaps[k]);
                                if (color.A != 0)
                                {
                                    //Vytvoření pixelu, který je posunutý v nové bitmapě 
                                    imageManipulation.AddPixel(i - leftPixelX, j - topPixelY, color, newBitmap);
                                }
                            }
                        }
                    }
                    bitmaps[k] = newBitmap;
                }
            }
        }

        public void CenterAlligment(List<WriteableBitmap> bitmaps)
        {
            for(int k = 0; k < bitmaps.Count; k++)
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
                        Color color = imageManipulation.GetPixelColor(i, j, bitmaps[k]);
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
                        Color color = imageManipulation.GetPixelColor(i, j, bitmaps[k]);
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
                    bitmaps[k].Clear();

                    //Zapsání pixelů z staré bitmapy do nové
                    for (int i = 0; i < croppedWidth; i++)
                    {
                        for (int j = 0; j < croppedHeight; j++)
                        {
                            Color color = imageManipulation.GetPixelColor(i, j, newBitmap);
                            imageManipulation.AddPixel(i + startPosX, j + startPosY, color, bitmaps[k]);
                        }
                    }

                    bitmaps[k] = newBitmap;
                }
            }
        }

        public void Resize(List<WriteableBitmap> bitmaps, int newWidth, int newHeight, string position)
        {
            int width = bitmaps[0].PixelWidth;
            int height = bitmaps[0].PixelHeight;

            if (newWidth != 0 && newHeight != 0)
            {
                //Získání pixelů z aktuální bitmapy
                int croppedWidth;
                int croppedHeight;
                //startPos je souřadnice zajišťující posun do zkrácené bitmapy při zmenšení 
                int startPosX = 0;
                int startPosY = 0;
                //endPos je souřadnice zajišťující posun do finální bitmapy při zvětšení 
                int endPosX = 0;
                int endPosY = 0;

                if (newWidth < width)
                {
                    croppedWidth = newWidth;
                    if (position.Contains("ĺeft")) startPosX = 0;
                    else if (position.Contains("middle")) startPosX = (width / 2) - (newWidth / 2);
                    else if (position.Contains("right")) startPosX = width - newWidth;
                }
                else
                {
                    croppedWidth = width;
                    if (position.Contains("ĺeft")) endPosX = 0;
                    else if (position.Contains("middle")) endPosX = (newWidth - width) / 2;
                    else if (position.Contains("right")) endPosX = newWidth - width;
                }

                if (newHeight < height)
                {
                    croppedHeight = newHeight;
                    if (position.Contains("top")) startPosY = 0;
                    else if (position.Contains("middle")) startPosY = (height / 2) - (newHeight / 2);
                    else if (position.Contains("bottom")) startPosY = height - newHeight;
                }
                else
                {
                    croppedHeight = height;
                    if (position.Contains("top")) endPosY = 0;
                    else if (position.Contains("middle")) endPosY = (newHeight - height) / 2;
                    else if (position.Contains("bottom")) endPosY = newHeight - height;
                }

                Int32Rect rect = new Int32Rect(startPosX, startPosY, croppedWidth, croppedHeight);

                for (int k = 0; k < bitmaps.Count; k++)
                {
                    CroppedBitmap croppedBitmap = new CroppedBitmap(bitmaps[k], rect);
                    WriteableBitmap newBitmap = new WriteableBitmap(croppedBitmap);
                    WriteableBitmap finalBitmap = BitmapFactory.New(newWidth, newHeight);

                    //Zapsání pixelů z staré bitmapy do nové
                    using (newBitmap.GetBitmapContext())
                    {
                        for (int i = 0; i < croppedWidth; i++)
                        {
                            for (int j = 0; j < croppedHeight; j++)
                            {
                                Color color = imageManipulation.GetPixelColor(i, j, newBitmap);
                                imageManipulation.AddPixel(i + endPosX, j + endPosY, color, finalBitmap);
                            }
                        }
                    }

                    bitmaps[k] = finalBitmap;
                }
            }
        }

        public WriteableBitmap CreateCompositeBitmap(List<WriteableBitmap> bitmaps)
        {
            int width = bitmaps[0].PixelWidth;
            int height = bitmaps[0].PixelHeight;
            int finalWidth = width;
            int finalHeight = height * bitmaps.Count;
            WriteableBitmap finalBitmap = BitmapFactory.New(finalWidth, finalHeight);
            using (finalBitmap.GetBitmapContext())
            {
                for (int k = 0; k < bitmaps.Count; k++)
                {
                    for (int i = 0; i < width; i++)
                    {
                        for (int j = 0; j < height; j++)
                        {
                            Color color = imageManipulation.GetPixelColor(i, j, bitmaps[k]);
                            imageManipulation.AddPixel(i, j + (k * height), color, finalBitmap);
                        }
                    }
                }
            }
            return finalBitmap;
        }
    }
}
