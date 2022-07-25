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
    internal class ResizeTransform
    {
        public void GenerateTransform(List<List<WriteableBitmap>> layers, int newWidth, int newHeight, string position)
        {
            List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();

            foreach (List<WriteableBitmap> layer in layers) 
            {
                foreach (WriteableBitmap bitmap in layer)
                {
                    bitmaps.Add(bitmap);
                }
            }

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
                    if (position.Contains("left")) startPosX = 0;
                    else if (position.Contains("right")) startPosX = width - newWidth;
                    else if (position.Contains("middle")) startPosX = (width / 2) - (newWidth / 2);
                }
                else
                {
                    croppedWidth = width;
                    if (position.Contains("left")) endPosX = 0;
                    else if (position.Contains("right")) endPosX = newWidth - width;
                    else if (position.Contains("middle")) endPosX = (newWidth - width) / 2;
                }

                if (newHeight < height)
                {
                    croppedHeight = newHeight;
                    if (position.Contains("top")) startPosY = 0;
                    else if (position.Contains("bottom")) startPosY = height - newHeight;
                    else if (position.Contains("middle")) startPosY = (height / 2) - (newHeight / 2);
                }
                else
                {
                    croppedHeight = height;
                    if (position.Contains("top")) endPosY = 0;
                    else if (position.Contains("bottom")) endPosY = newHeight - height;
                    else if (position.Contains("middle")) endPosY = (newHeight - height) / 2;
                }

                Int32Rect rect = new Int32Rect(startPosX, startPosY, croppedWidth, croppedHeight);

                for (int i = 0; i < layers.Count; i++)
                {
                    for (int j = 0; j < layers[i].Count; j++)
                    {
                        CroppedBitmap croppedBitmap = new CroppedBitmap(layers[i][j], rect);
                        WriteableBitmap newBitmap = new WriteableBitmap(croppedBitmap);
                        WriteableBitmap finalBitmap = BitmapFactory.New(newWidth, newHeight);

                        //Zapsání pixelů ze staré bitmapy do nové
                        using (newBitmap.GetBitmapContext())
                        {
                            for (int x = 0; x < croppedWidth; x++)
                            {
                                for (int y = 0; y < croppedHeight; y++)
                                {
                                    Color color = newBitmap.GetPixel(x, y);
                                    finalBitmap.SetPixel(x + endPosX, y + endPosY, color);
                                }
                            }
                        }

                        layers[i][j] = finalBitmap;
                    }
                }
            }
        }
    }
}
