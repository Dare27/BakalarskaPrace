using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Forms;
using System.Windows.Controls.Primitives;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Collections.Generic;
using BakalarskaPrace.Properties;
using System.Drawing.Imaging;
using System.IO;

namespace BakalarskaPrace
{
    public partial class MainWindow : Window
    {
        private WriteableBitmap currentBitmap;
        int currentBitmapIndex = 0;
        private readonly List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();
        private readonly WriteableBitmap defaultBitmap;
        int colorPalleteSize = 2;
        Color[] colorPallete;
        int strokeThickness = 1;
        byte alpha = 255;
        enum tools {brush, eraser, symmetricBrush, colorPicker};
        tools currentTool = tools.brush;
        const double scaleRate = 1.1;
        Point gridDragStartPoint;
        Vector gridDragOffset;
        int width;
        int height;
        int defaultWidth = 64;
        int defaultHeight = 64;
        double currentScale = 1.0;

        private System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        int timerInterval = 1000; 
        int currentAnimationIndex;
        int currentFPSTarget = 12;

        Color defaultPreviewColor;
        
        public MainWindow()
        {
            colorPallete = new Color[colorPalleteSize];
            colorPallete[0] = Color.FromArgb(alpha, 0, 0, 0);         //Primární barva
            colorPallete[1] = Color.FromArgb(alpha, 255, 255, 255);   //Sekundární barva
            defaultPreviewColor = Color.FromArgb(128, 178, 213, 226);
            width = defaultWidth;
            height = defaultHeight;
            defaultBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
            currentBitmap = defaultBitmap.Clone();
            bitmaps.Add(currentBitmap);
            InitializeComponent();
            image.Source = bitmaps[currentBitmapIndex];
            LabelPosition.Content = "[" + width + ":" + height + "] " + 0 + ":" + 0;
            LabelScale.Content = "1.0";
            LabelImages.Content = bitmaps.Count.ToString() + ":"+ (currentBitmapIndex + 1).ToString();

            timer.Tick += new EventHandler(OnTimedEvent);
            timerInterval = 1000 / currentFPSTarget;
            timer.Interval = new TimeSpan(0, 0, 0, 0, timerInterval);
            timer.Start();
        }

        private void OnTimedEvent(object sender, EventArgs e)
        {
            if (currentAnimationIndex + 1 < bitmaps.Count)
            {
                currentAnimationIndex += 1;
            }
            else
            {
                currentAnimationIndex = 0;
            }

            animationPreview.Source = bitmaps[currentAnimationIndex];
        }

        private unsafe void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;

            LabelPosition.Content = "[" + width + ":" + height + "] " + x + ":" + y;

            //AddPixel(x, y, defaultPreviewColor);

            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
            {
                switch (currentTool)
                {
                    case tools.brush:
                        {
                            int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;

                            AddPixel(x, y, colorPallete[colorIndex]);
                            if (System.Windows.Forms.Control.ModifierKeys != Keys.Control)
                            {
                                Color currentPixelColor = GetPixelColor(x, y);
                                Color colorMix = ColorMix(colorPallete[colorIndex], currentPixelColor);
                                AddPixel(x, y, colorMix);
                                /*if (currentPixelColor.A != 0)
                                {
                                    
                                }
                                else
                                {
                                    AddPixel(x, y, colorPallete[colorIndex]);
                                }*/
                            }
                            else
                            {
                                AddPixel(x, y, colorPallete[colorIndex]);
                            }
                            break;
                        }
                    case tools.symmetricBrush:
                        {
                            int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;
                            int mirrorPostion = 0;

                            //Chybí převrácení podle osy souměrnosti
                            if (System.Windows.Forms.Control.ModifierKeys == Keys.Shift)
                            {
                                AddPixel(x, y, colorPallete[colorIndex]);

                                //Použít horizontální a vertikální osu 
                                if (x > currentBitmap.PixelWidth / 2)
                                {
                                    mirrorPostion = currentBitmap.PixelWidth - x - 1;
                                    AddPixel(mirrorPostion, y, colorPallete[colorIndex]);
                                }
                                else 
                                {

                                    int dif = (currentBitmap.PixelWidth / 2) - x;
                                    mirrorPostion = (currentBitmap.PixelWidth / 2) + dif - 1;
                                    AddPixel(mirrorPostion, y, colorPallete[colorIndex]);
                                }

                                if (y > currentBitmap.PixelHeight / 2)
                                {
                                    mirrorPostion = currentBitmap.PixelHeight - y - 1;
                                    AddPixel(x, mirrorPostion, colorPallete[colorIndex]);
                                }
                                else
                                {
                                    int dif = (currentBitmap.PixelHeight / 2) - y;
                                    mirrorPostion = (currentBitmap.PixelHeight / 2) + dif - 1;
                                    AddPixel(x, mirrorPostion, colorPallete[colorIndex]);
                                }
                            }
                            else if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                            {
                                AddPixel(x, y, colorPallete[colorIndex]);

                                //Použít horizontální osu 
                                if (y > currentBitmap.PixelHeight / 2)
                                {
                                    mirrorPostion = currentBitmap.PixelHeight - y - 1;
                                    AddPixel(x, mirrorPostion, colorPallete[colorIndex]);
                                }
                                else
                                {
                                    int dif = (currentBitmap.PixelHeight / 2) - y;
                                    mirrorPostion = (currentBitmap.PixelHeight / 2) + dif - 1;
                                    AddPixel(x, mirrorPostion, colorPallete[colorIndex]);
                                }
                            }
                            else
                            {
                                //Použít vertikální osu 
                                if (x > currentBitmap.PixelWidth / 2)
                                {
                                    mirrorPostion = currentBitmap.PixelWidth - x - 1;
                                    AddPixel(mirrorPostion, y, colorPallete[colorIndex]);
                                }
                                else
                                {
                                    int dif = (currentBitmap.PixelWidth / 2) - x;
                                    mirrorPostion = (currentBitmap.PixelWidth / 2) + dif - 1;
                                    AddPixel(mirrorPostion, y, colorPallete[colorIndex]);
                                }
                            }
                            AddPixel(x, y, colorPallete[colorIndex]);
                            
                            break;
                        }
                    case tools.eraser:
                        {
                            byte[] ColorData = {0, 0, 0, 0}; // B G R

                            Int32Rect rect = new Int32Rect(x, y, 1, 1);

                            currentBitmap.WritePixels(rect, ColorData, 4, 0);
                            break;
                        }
                    case tools.colorPicker:
                        {
                            int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;
                            colorPallete[colorIndex] = GetPixelColor(x, y);
                            Console.WriteLine(colorPallete[colorIndex]);
                            SolidColorBrush brush = new SolidColorBrush();
                            brush.Color = colorPallete[colorIndex];
                            if (colorIndex == 0)
                            {
                                ColorSelector0.Background = brush;
                            }
                            else 
                            {
                                ColorSelector1.Background = brush;
                            }
                            break;
                        }
                    default: break;
                }
            }
        }

        private void AddPixel(int x, int y, Color color, bool AlphaBlend = true) 
        {
            try
            {
                // Reserve the back buffer for updates.
                currentBitmap.Lock();
                unsafe
                {
                    IntPtr pBackBuffer = currentBitmap.BackBuffer;

                    pBackBuffer += y * currentBitmap.BackBufferStride;
                    pBackBuffer += x * 4;

                    int colorData = color.A << 24;       // A
                    colorData |= color.R << 16;          // R
                    colorData |= color.G << 8;           // G
                    colorData |= color.B << 0;           // B

                    // Assign the color data to the pixel.
                    *((int*)pBackBuffer) = colorData;

                    currentBitmap.AddDirtyRect(new Int32Rect(0, 0, currentBitmap.PixelWidth, currentBitmap.PixelHeight));
                }
            }
            finally
            {
                // Release the back buffer and make it available for display.
                currentBitmap.Unlock();
            }
        }

        unsafe Color GetPixelColor(int x, int y)
        {
            Color pix = new Color();
            byte[] colorData = { 0, 0, 0, 0 }; // ARGB
            IntPtr pBackBuffer = currentBitmap.BackBuffer;
            byte* pBuff = (byte*)pBackBuffer.ToPointer();
            var a = pBuff[4 * x + (y * currentBitmap.BackBufferStride) + 3];
            var r = pBuff[4 * x + (y * currentBitmap.BackBufferStride) + 2];
            var g = pBuff[4 * x + (y * currentBitmap.BackBufferStride) + 1];
            var b = pBuff[4 * x + (y * currentBitmap.BackBufferStride)];
            pix.A = a;
            pix.R = r;
            pix.G = g;
            pix.B = b;
            return pix;
        }

        public static Color ColorMix(Color firstColor, Color secondColor, float percent = .5f)
        {
            byte a = (byte)(firstColor.A + firstColor.A * (1 - firstColor.A));
            byte r = (byte)((firstColor.R * firstColor.A + secondColor.R * secondColor.A * (1 - firstColor.A)) / a);
            byte g = (byte)((firstColor.G * firstColor.A + secondColor.G * secondColor.A * (1 - firstColor.A)) / a);
            byte b = (byte)((firstColor.B * firstColor.A + secondColor.B * secondColor.A * (1 - firstColor.A)) / a);

            return Color.FromArgb(a,r,g,b);
        }

        private void Eraser_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.eraser;
        }

        private void Brush_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.brush;
        }

        private void SymmetricBrush_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.symmetricBrush;
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            UIElement element = (UIElement)sender;
            Point position = e.GetPosition(element);
            MatrixTransform transform = (MatrixTransform)element.RenderTransform;
            Matrix matrix = transform.Matrix;
            double scale;

            // Pokud je e >= 0 dojde k přibližování
            if (e.Delta >= 0)
            {
                scale = 1.1;
            }
            else 
            {
                scale = 1.0 / 1.1;
            }

            matrix.ScaleAtPrepend(scale, scale, position.X, position.Y);
            transform.Matrix = matrix;

            currentScale *= scale;
            if (currentScale.ToString().Length > 5)
            {
                LabelScale.Content = currentScale.ToString().Substring(0, 5);
            }
            else 
            {
                LabelScale.Content = currentScale.ToString();
            }
        }
        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle) 
            {
                gridDragStartPoint = e.GetPosition(window);
                gridDragOffset = new Vector(Grid_TranslateTransform.X, Grid_TranslateTransform.Y);
                grid.CaptureMouse();
            }
        }

        private void Grid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (grid.IsMouseCaptured)
            {
                Vector offset = Point.Subtract(e.GetPosition(window), gridDragStartPoint);

                Grid_TranslateTransform.X = gridDragOffset.X + offset.X;
                Grid_TranslateTransform.Y = gridDragOffset.Y + offset.Y;
            }
        }

        private void Grid_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Middle)
            {
                grid.ReleaseMouseCapture();
            }
        }

        private void ColorSelection_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.Button button = sender as System.Windows.Controls.Button;
            String buttonName = button.Name.ToString();
            buttonName = Regex.Replace(buttonName, "[^0-9]", "");
            ColorDialog colorDialog = new ColorDialog();

            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) 
            {
                System.Drawing.Color color = colorDialog.Color;
                colorPallete[Int32.Parse(buttonName)] = System.Windows.Media.Color.FromArgb(alpha, color.R, color.G, color.B);
                SolidColorBrush brush = new SolidColorBrush();
                brush.Color = colorPallete[Int32.Parse(buttonName)];
                button.Background = brush;
            }
        }

        private void BrushSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            strokeThickness = (int)e.NewValue;
        }

        private void Transparency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            alpha = (byte)e.NewValue;
            for (int i = 0; i < colorPallete.Length; i++) 
            {
                colorPallete[i].A = alpha;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    if (dialog.FileName != string.Empty)
                    {
                        using (FileStream fileStream = new FileStream(dialog.FileName, FileMode.Create))
                        {
                            PngBitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(bitmaps[currentBitmapIndex].Clone()));
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

        private void Load_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.Filter = "png images *(.png)|*.png;";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {
                    //System.Drawing.Bitmap myBitmap = new System.Drawing.Bitmap(dialog.FileName);
                }
                catch
                {

                }
            }
        }

        private void CreateImage_Click(object sender, RoutedEventArgs e)
        {
            CreateNewFrame(false);
        }

        private void DuplicateImage_Click(object sender, RoutedEventArgs e)
        {
            CreateNewFrame(true);
        }

        private void CreateNewFrame(bool duplicate) 
        {
            WriteableBitmap newWriteableBitmap;

            if (duplicate)
            {
                newWriteableBitmap = bitmaps[currentBitmapIndex].Clone();
            }
            else 
            {
                newWriteableBitmap = defaultBitmap.Clone();
            }

            if (currentBitmapIndex < bitmaps.Count - 1)
            {
                bitmaps.Insert(currentBitmapIndex + 1, newWriteableBitmap);
            }
            else
            {
                bitmaps.Add(newWriteableBitmap);
            }

            currentBitmapIndex += 1;
            currentBitmap = bitmaps[currentBitmapIndex];
            image.Source = currentBitmap;
            LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
            currentAnimationIndex = currentBitmapIndex;
            animationPreview.Source = bitmaps[currentAnimationIndex];
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmapIndex - 1 > -1)
            {
                currentBitmapIndex -= 1;
                currentBitmap = bitmaps[currentBitmapIndex];
                image.Source = currentBitmap;
                LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
                currentAnimationIndex = currentBitmapIndex;
                animationPreview.Source = bitmaps[currentAnimationIndex];
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmapIndex + 1 < bitmaps.Count) 
            {
                currentBitmapIndex += 1;
                currentBitmap = bitmaps[currentBitmapIndex];
                image.Source = currentBitmap;
                LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
                currentAnimationIndex = currentBitmapIndex;
                animationPreview.Source = bitmaps[currentAnimationIndex];
            }
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            bitmaps.RemoveAt(currentBitmapIndex);
            if (bitmaps.Count == 0)
            {
                WriteableBitmap newWriteableBitmap = defaultBitmap.Clone();
                bitmaps.Add(newWriteableBitmap);
            }
            //pokud je poslední index vrátit se na předchozí obrázek
            int lastIndex = currentBitmapIndex != bitmaps.Count ? 0 : 1;
            currentBitmapIndex -= lastIndex;
            currentBitmap = bitmaps[currentBitmapIndex];
            image.Source = currentBitmap;
            LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
            currentAnimationIndex = currentBitmapIndex;
            animationPreview.Source = bitmaps[currentAnimationIndex];
        }

        private void FramesPerSecond_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = sender as Slider;
            double value = slider.Value;
            LabelFramesPerSecond.Content = value.ToString();
            currentFPSTarget = (int)value;
            if (currentFPSTarget != 0)
            {
                timerInterval = 1000 / currentFPSTarget;
                timer.Stop();
                timer.Interval = new TimeSpan(0, 0, 0, 0, timerInterval);
                timer.Start();
            }
            else 
            {
                timer.Stop();
                currentAnimationIndex = currentBitmapIndex;
                animationPreview.Source = bitmaps[currentAnimationIndex];
            }
        }

        private void ColorPicker_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.colorPicker;
        }
    }
}