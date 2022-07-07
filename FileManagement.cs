using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace
{
    internal class FileManagement
    {
        public void SaveFile(List<List<WriteableBitmap>> layers, int width, int height)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = ".pixela|*.pixela";
            DateTime localDate = DateTime.Now;
            string format = "H-mm-d-M-yyyy";
            string result = localDate.ToString(format);
            string name = "New Animation " + result;
            dialog.FileName = name;
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    if (dialog.FileName != string.Empty)
                    {
                        using (StreamWriter streamWriter = new StreamWriter(dialog.FileName))
                        {
                            streamWriter.WriteLine(/*"Width=" + */width);
                            streamWriter.WriteLine(/*"Height=" + */height);
                            streamWriter.WriteLine(/*"Layers=" + */layers.Count);
                            streamWriter.WriteLine(/*"Frames=" + */layers[0].Count);
                            for (int i = 0; i < layers.Count; i++) 
                            {
                                for (int j = 0; j < layers[i].Count; j++) 
                                {
                                    byte[] buffer = layers[i][j].ToByteArray();
                                    string line = Encoding.Unicode.GetString(buffer);
                                    streamWriter.WriteLine(line); 
                                }
                            }
                        }
                    }
                }
                catch
                {

                }
            }
        }

        public List<List<WriteableBitmap>> LoadFile()
        {
            List<List<WriteableBitmap>> newLayers = new List<List<WriteableBitmap>>();
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = ".pixela|*.pixela;";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    using (StreamReader readtext = new StreamReader(dialog.FileName))
                    {
                        string[] lines = File.ReadAllLines(dialog.FileName);
                        int width = int.Parse(lines[0]);
                        int height = int.Parse(lines[1]);
                        int layersCount = Convert.ToInt32(lines[2]);
                        int framesCount = Convert.ToInt32(lines[3]);
                        List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();

                        for (int i = 4; i < lines.Count(); i++)
                        {
                            if (lines[i] != "")
                            {
                                byte[] buffer = Encoding.Unicode.GetBytes(lines[i]);
                                WriteableBitmap bitmap = BitmapFactory.New(width, height);
                                bitmap.FromByteArray(buffer);
                                bitmaps.Add(bitmap);
                            }
                        }

                        for (int i = 0; i < layersCount; i++)
                        {
                            List<WriteableBitmap> newLayer = new List<WriteableBitmap>();
                            newLayers.Add(newLayer);
                            for (int j = 0; j < framesCount; j++)
                            {
                                newLayers[i].Add(bitmaps[j + framesCount * i]);
                            }
                        }
                        return newLayers;
                    }

                }
                catch
                {

                }
            }
            return null;
        }

        public List<List<WriteableBitmap>> ImportFile()
        {
            List<List<WriteableBitmap>> newLayers = new List<List<WriteableBitmap>>();
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "png images *(.png)|*.png;";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    var filePath = dialog.FileName;
                    BitmapImage bitmapImage = new BitmapImage(new Uri(filePath));
                    WriteableBitmap newBitmap = new WriteableBitmap(bitmapImage);
                    WindowLoadImage subwindow = new WindowLoadImage();
                    subwindow.ShowDialog();
                    List<WriteableBitmap> layer = new List<WriteableBitmap>();
                    newLayers.Add(layer);

                    if (subwindow.importImage || subwindow.importSpritesheet)
                    {
                        if (subwindow.importImage == true)
                        {
                            newLayers[0].Add(newBitmap);
                        }
                        else
                        {
                            //Vydělení strany animace velikostí snímku
                            int rows = newBitmap.PixelWidth / subwindow.imageWidth;
                            int columns = newBitmap.PixelHeight / subwindow.imageHeight;
                            int offsetWidth = subwindow.offsetWidth;
                            int offsetHeight = subwindow.offsetWidth;

                            //Získání jednotlivých snímků 
                            for (int j = 0; j < rows; j++)
                            {
                                for (int i = 0; i < columns; i++)
                                {
                                    Int32Rect rect = new Int32Rect(i * subwindow.imageWidth, j * subwindow.imageHeight, subwindow.imageWidth, subwindow.imageHeight);
                                    CroppedBitmap croppedBitmap = new CroppedBitmap(newBitmap, rect);
                                    WriteableBitmap writeableBitmap = new WriteableBitmap(croppedBitmap);
                                    newLayers[0].Add(writeableBitmap);
                                }
                            }
                        }
                        return newLayers;
                    }
                }
                catch
                {

                }
            }
            return null;
        }

        public void ExportPNG(WriteableBitmap bitmap)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = ".png|*.png|.bmp|*.bmp|.jpeg|*.jpeg";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    if (dialog.FileName != string.Empty)
                    {
                        using (FileStream fileStream = new FileStream(dialog.FileName, FileMode.Create))
                        {
                            PngBitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmap.Clone()));
                            encoder.Save(fileStream);
                            fileStream.Close();
                            fileStream.Dispose();
                        }
                    }
                }
                catch
                {

                }
            }
        }

        public void ExportPSD(List<WriteableBitmap> layers/*, WriteableBitmap combinedBitmap*/)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = ".psd|*.psd";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    if (dialog.FileName != string.Empty)
                    {
                        using (MagickImageCollection collection = new MagickImageCollection())
                        {
                            //Pro tento formát by měla být správně jako první obrázek kombinace všech vrstev
                            WriteableBitmap emptyBitmap = BitmapFactory.New((int)layers[0].Width, (int)layers[0].Height);
                            MagickImage imgBase = new MagickImage(ImageToByte(emptyBitmap));
                            collection.Add(imgBase);
                            for (int i = 0; i < layers.Count; i++)
                            {
                                //Převedení WriteableBitmap na byte pole
                                byte[] bitmapData = ImageToByte(layers[i]);
                                MagickImage image = new MagickImage(bitmapData);
                                collection.Add(image);
                            }
                            collection.Write(System.IO.Path.GetFullPath(dialog.FileName));
                        }
                    }
                }
                catch
                {

                }
            }
        }

        public void ExportGif(List<WriteableBitmap> bitmaps, int timerInterval)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Filter = ".gif|*.gif";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    if (dialog.FileName != string.Empty)
                    {
                        using (MagickImageCollection collection = new MagickImageCollection())
                        {
                            //Výpočet rozestupu mezi snímky
                            int delay = timerInterval / 10;

                            for (int i = 0; i < bitmaps.Count; i++)
                            {
                                //Převedení WriteableBitmap na byte pole
                                byte[] bitmapData = ImageToByte(bitmaps[i]);
                                MagickImage image = new MagickImage(bitmapData);
                                collection.Add(image);
                                collection[i].AnimationDelay = delay;
                            }

                            //Snížení množství barev
                            QuantizeSettings settings = new QuantizeSettings();
                            settings.Colors = 256;
                            collection.Quantize(settings);

                            // Volitelné optimalizování obrázků, správně by obrázky měly mít stejnou velikost
                            collection.Optimize();
                            collection.Write(System.IO.Path.GetFullPath(dialog.FileName));
                        }
                    }
                }
                catch
                {

                }
            }
        }

        public List<Color> ImportColorPalette()
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "png images *(.png)|*.png;";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    List<Color> colorPalette = new List<Color>();
                    var filePath = dialog.FileName;
                    BitmapImage bitmapImage = new BitmapImage(new Uri(filePath));
                    WriteableBitmap newBitmap = new WriteableBitmap(bitmapImage);
                    if (newBitmap.PixelHeight < 2 && newBitmap.PixelWidth < 257)
                    {
                        using (newBitmap.GetBitmapContext())
                        {
                            ImageManipulation imageManipulation = new ImageManipulation();
                            for (int i = 0; i < newBitmap.PixelWidth; i++)
                            {
                                Color color = imageManipulation.GetPixelColor(i, 0, newBitmap);
                                colorPalette.Add(color);
                            }
                            return colorPalette;
                        }
                    }
                }
                catch
                {

                }
            }
            return null;
        }

        public static byte[] ImageToByte(WriteableBitmap imageSource)
        {
            var encoder = new BmpBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageSource));

            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                return ms.ToArray();
            }
        }
    }
}
