using ImageMagick;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace
{
    internal class FileManagement
    {
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
