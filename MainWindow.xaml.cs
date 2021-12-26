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
        private readonly WriteableBitmap bitmap;
        private readonly WriteableBitmap defaultBitmap = new WriteableBitmap(64, 64, 1, 1, PixelFormats.Bgra32, null);
        int colorPalleteSize = 2;
        Color[] colorPallete;
        int strokeThickness = 1;
        byte alpha = 255;
        enum tools {brush, eraser, symmetricBrush};
        tools currentTool = tools.brush;
        const double scaleRate = 1.1;
        Point gridDragStartPoint;
        Vector gridDragOffset;
        int width;
        int height;
        int defaultWidth = 64;
        int defaultHeight = 64;
        double currentScale = 1.0;

        public MainWindow()
        {
            colorPallete = new Color[colorPalleteSize];
            colorPallete[0] = Color.FromArgb(alpha, 0, 0, 0);         //Primární barva
            colorPallete[1] = Color.FromArgb(alpha, 255, 255, 255);   //Sekundární barva
            width = defaultWidth;
            height = defaultHeight;
            bitmap = defaultBitmap;
            InitializeComponent();
            image.Source = bitmap;
            LabelPosition.Content = "[" + bitmap.PixelWidth + ":" + bitmap.PixelHeight + "] " + 0 + ":" + 0;
            LabelScale.Content = "1.0";
        }

        private unsafe void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;

            LabelPosition.Content = "[" + bitmap.PixelWidth + ":" + bitmap.PixelHeight + "] " + x + ":" + y;

            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
            {
                switch (currentTool)
                {
                    case tools.brush:
                        {
                            int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;

                            AddPixel(x, y, colorIndex);
                            break;
                        }
                    case tools.symmetricBrush:
                        {
                            int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;
                            int mirrorPostion = 0;

                            if (System.Windows.Forms.Control.ModifierKeys == Keys.Shift)
                            {
                                AddPixel(x, y, colorIndex);

                                //Použít horizontální a vertikální osu 
                                if (x > bitmap.PixelWidth / 2)
                                {
                                    mirrorPostion = bitmap.PixelWidth - x - 1;
                                    AddPixel(mirrorPostion, y, colorIndex);
                                }
                                else 
                                {

                                    int dif = (bitmap.PixelWidth / 2) - x;
                                    mirrorPostion = (bitmap.PixelWidth / 2) + dif - 1;
                                    AddPixel(mirrorPostion, y, colorIndex);
                                }

                                if (y > bitmap.PixelHeight / 2)
                                {
                                    mirrorPostion = bitmap.PixelHeight - y - 1;
                                    AddPixel(x, mirrorPostion, colorIndex);
                                }
                                else
                                {
                                    int dif = (bitmap.PixelHeight / 2) - y;
                                    mirrorPostion = (bitmap.PixelHeight / 2) + dif - 1;
                                    AddPixel(x, mirrorPostion, colorIndex);
                                }
                            }
                            else if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                            {
                                AddPixel(x, y, colorIndex);

                                //Použít horizontální osu 
                                if (y > bitmap.PixelHeight / 2)
                                {
                                    mirrorPostion = bitmap.PixelHeight - y - 1;
                                    AddPixel(x, mirrorPostion, colorIndex);
                                }
                                else
                                {
                                    int dif = (bitmap.PixelHeight / 2) - y;
                                    mirrorPostion = (bitmap.PixelHeight / 2) + dif - 1;
                                    AddPixel(x, mirrorPostion, colorIndex);
                                }
                            }
                            else
                            {
                                //Použít vertikální osu 
                                if (x > bitmap.PixelWidth / 2)
                                {
                                    mirrorPostion = bitmap.PixelWidth - x - 1;
                                    AddPixel(mirrorPostion, y, colorIndex);
                                }
                                else
                                {
                                    int dif = (bitmap.PixelWidth / 2) - x;
                                    mirrorPostion = (bitmap.PixelWidth / 2) + dif - 1;
                                    AddPixel(mirrorPostion, y, colorIndex);
                                }
                            }
                            AddPixel(x, y, colorIndex);
                            
                            break;
                        }
                    case tools.eraser:
                        {
                            byte[] ColorData = {0, 0, 0, 0}; // B G R

                            Int32Rect rect = new Int32Rect(x, y, 1, 1);

                            bitmap.WritePixels(rect, ColorData, 4, 0);
                            break;
                        }
                    default: break;
                }
                
            }
        }

        private void AddPixel(int x, int y, int colorIndex, bool horizontal = false, bool vertical = false) 
        {
            try
            {
                // Reserve the back buffer for updates.
                bitmap.Lock();

                unsafe
                {
                    IntPtr pBackBuffer = bitmap.BackBuffer;

                    pBackBuffer += y * bitmap.BackBufferStride;
                    pBackBuffer += x * 4;

                    int color_data = colorPallete[colorIndex].A << 24;       // A
                    color_data |= colorPallete[colorIndex].R << 16;          // R
                    color_data |= colorPallete[colorIndex].G << 8;           // G
                    color_data |= colorPallete[colorIndex].B << 0;           // B

                    // Assign the color data to the pixel.
                    *((int*)pBackBuffer) = color_data;

                    bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                }
            }
            finally
            {
                // Release the back buffer and make it available for display.
                bitmap.Unlock();
            }
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
    }
}