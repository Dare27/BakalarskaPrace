using ImageMagick;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
