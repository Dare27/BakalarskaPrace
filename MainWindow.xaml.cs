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
using System.Linq;
using System.Numerics; 

namespace BakalarskaPrace
{
    public partial class MainWindow : Window
    {
        private WriteableBitmap currentBitmap;
        int currentBitmapIndex = 0;
        private readonly List<WriteableBitmap> bitmaps = new List<WriteableBitmap>();
        private readonly List<System.Windows.Controls.Button> previewButtons = new List<System.Windows.Controls.Button>();
        private readonly WriteableBitmap defaultBitmap;
        int colorPalleteSize = 2;
        Color[] colorPallete;
        ColorSpaceConvertor colorSpaceConvertor = new ColorSpaceConvertor();
        int strokeThickness = 1;
        byte alpha = 255;
        bool alphaBlending = true;
        bool additive = false;
        enum tools { brush, eraser, symmetricBrush, colorPicker, bucket, specialBucket, line, ellipsis, shading, rectangle, dithering, move };
        tools currentTool = tools.brush;
        const double scaleRate = 1.1;
        Point gridDragStartPoint;
        System.Windows.Vector gridDragOffset;
        int width;
        int height;
        double currentScale = 1.0;
        Vector2 mousePosition = new Vector2(0, 0);
        Vector2 previousMousePosition = new Vector2(0, 0);
        Vector2 previewMousePosition = new Vector2(0, 0);
        Vector2 mouseDownPosition = new Vector2(0, 0);
        
        int currentColorIndex = 0;
        double shadingStep = .1;
        bool onionSkinning;
        System.Windows.Controls.Button lastToolButton;

        Color defaultPreviewColor;
        WriteableBitmap previewBitmap;
        Image previewImage = new Image();

        private System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        int timerInterval = 1000;
        int currentAnimationIndex;
        int currentFPSTarget = 12;

        public MainWindow()
        {
            colorPallete = new Color[colorPalleteSize];
            colorPallete[0] = Color.FromArgb(alpha, 0, 0, 0);         //Primární barva
            colorPallete[1] = Color.FromArgb(alpha, 255, 255, 255);   //Sekundární barva
            defaultPreviewColor = Color.FromArgb(255, 178, 213, 226);
            InitializeComponent();
            this.Show();
            paintSurface.Visibility = Visibility.Hidden;
            WindowStartup windowStartup = new WindowStartup();
            windowStartup.ShowDialog();

            paintSurface.Visibility = Visibility.Visible;
            width = windowStartup.newWidth;
            height = windowStartup.newHeight;
            defaultBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);

            previewBitmap = defaultBitmap.Clone();
            previewImage.Source = previewBitmap;
            previewImage.Opacity = 0.75f;
            previewImage.Width = windowStartup.newWidth; ;
            previewImage.Height = windowStartup.newHeight;
            RenderOptions.SetBitmapScalingMode(previewImage, BitmapScalingMode.NearestNeighbor);
            paintSurface.Children.Add(previewImage);
            
            currentBitmap = defaultBitmap.Clone();
            bitmaps.Add(currentBitmap);
            paintSurface.Width = width;
            paintSurface.Height = height;
            image.Width = width;
            image.Height = height;
            image.Source = bitmaps[currentBitmapIndex];
            LabelPosition.Content = "[" + width + ":" + height + "] " + 0 + ":" + 0;
            LabelScale.Content = "1.0";
            LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
            UpdateImagePreviewButtons(); 

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

        private unsafe void Image_MouseDown(object sender, System.Windows.Input.MouseEventArgs e)
        {
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;

            mousePosition = new Vector2(x, y);

            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
            {
                int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;
                switch (currentTool)
                {
                    case tools.brush:
                        {
                            if (additive == true)
                            {
                                StrokeThicknessSetter(x, y, colorPallete[colorIndex]);
                            }
                            else
                            {
                                if (previousMousePosition.X != x || previousMousePosition.Y != y)
                                {
                                    previousMousePosition = new Vector2(x, y);
                                    StrokeThicknessSetter(x, y, colorPallete[colorIndex]);
                                }
                            }
                            break;
                        }
                    case tools.symmetricBrush:
                        {
                            if (additive == true)
                            {
                                SymmetricDrawing(x, y, colorPallete[colorIndex]);
                            }
                            else
                            {
                                if (previousMousePosition.X != x || previousMousePosition.Y != y)
                                {
                                    previousMousePosition = new Vector2(x, y);
                                    SymmetricDrawing(x, y, colorPallete[colorIndex]);
                                }
                            }
                            break;
                        }
                    case tools.eraser:
                        {
                            Eraser(x, y, currentBitmap);
                            break;
                        }
                    case tools.colorPicker:
                        {
                            ColorPicker(x, y, colorIndex);
                            break;
                        }
                    case tools.bucket:
                        {
                            Color seedColor = GetPixelColor(x, y, currentBitmap);
                            FloodFill(x, y, colorPallete[colorIndex], seedColor);
                            break;
                        }
                    case tools.specialBucket:
                        {
                            if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                            {
                                foreach (WriteableBitmap writeableBitmap in bitmaps) 
                                {
                                    SpecialBucket(x, y, colorIndex, writeableBitmap);
                                }
                            }
                            else
                            {
                                SpecialBucket(x, y, colorIndex, currentBitmap);
                            }
                            break;
                        }
                    case tools.line:
                        {
                            mouseDownPosition = new Vector2(x, y);
                            currentColorIndex = colorIndex;
                            break;
                        }
                    case tools.ellipsis:
                        {
                            mouseDownPosition = new Vector2(x, y);
                            currentColorIndex = colorIndex;
                            break;
                        }
                    case tools.rectangle:
                        {
                            mouseDownPosition = new Vector2(x, y);
                            currentColorIndex = colorIndex;
                            break;
                        }
                    case tools.dithering:
                        {
                            Dithering(x, y, colorPallete[0], colorPallete[1]);
                            break;
                        }
                    case tools.shading:
                        {
                            if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                            {
                                //Zesvětlení
                                Lighten(x, y);
                            }
                            else
                            {
                                //Ztmavení 
                                Darken(x, y);
                            }
                            break;
                        }
                    default: break;
                }
            }
        }

        private unsafe void Image_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;

            mousePosition = new Vector2(x, y);

            LabelPosition.Content = "[" + width + ":" + height + "] " + x + ":" + y;
            int colorIndex = e.LeftButton == MouseButtonState.Pressed ? 0 : 1;

            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
            {
                switch (currentTool)
                {
                    case tools.brush:
                        {
                            if (additive == true)
                            {
                                StrokeThicknessSetter(x, y, colorPallete[colorIndex]);
                            }
                            else
                            {
                                if (previousMousePosition.X != x || previousMousePosition.Y != y)
                                {
                                    previousMousePosition = new Vector2(x, y);
                                    StrokeThicknessSetter(x, y, colorPallete[colorIndex]);
                                }
                            }
                            break;
                        }
                    case tools.symmetricBrush:
                        {
                            if (additive == true)
                            {
                                SymmetricDrawing(x, y, colorPallete[colorIndex]);
                            }
                            else
                            {
                                if (previousMousePosition.X != x || previousMousePosition.Y != y)
                                {
                                    previousMousePosition = new Vector2(x, y);
                                    SymmetricDrawing(x, y, colorPallete[colorIndex]);
                                }
                            }
                            break;
                        }
                    case tools.eraser:
                        {
                            Eraser(x, y, currentBitmap);
                            break;
                        }
                    case tools.colorPicker:
                        {
                            ColorPicker(x, y, colorIndex);
                            break;
                        }
                    case tools.bucket:
                        {
                            Color seedColor = GetPixelColor(x, y, currentBitmap);
                            FloodFill(x, y, colorPallete[colorIndex], seedColor);
                            break;
                        }
                    case tools.specialBucket:
                        {
                            if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                            {
                                foreach (WriteableBitmap writeableBitmap in bitmaps)
                                {
                                    SpecialBucket(x, y, colorIndex, writeableBitmap);
                                }
                            }
                            else
                            {
                                SpecialBucket(x, y, colorIndex, currentBitmap);
                            }
                            break;
                        }
                    case tools.dithering:
                        {
                            Dithering(x, y, colorPallete[0], colorPallete[1]);
                            break;
                        }
                    case tools.shading:
                        {
                            if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                            {
                                //Zesvětlení
                                Lighten(x, y);
                            }
                            else
                            {
                                //Ztmavení 
                                Darken(x, y);
                            }
                            break;
                        }
                    default: break;
                }
            }

            if (x >= 0 && y >= 0 && x < width && y < height && (previewMousePosition.X != x || previewMousePosition.Y != y))
            {
                /*Eraser((int)previewMousePosition.X, (int)previewMousePosition.Y, previewBitmap);
                previewMousePosition = new Vector2(x, y);
                AddPixel(x, y, defaultPreviewColor, previewBitmap);*/
            }
        }

        private unsafe void Image_MouseUp(object sender, System.Windows.Input.MouseEventArgs e)
        {
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;

            mousePosition = new Vector2(x, y);

            switch (currentTool)
            {
                case tools.line:
                    {
                        if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
                        {
                            DrawStraightLine((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x, y, colorPallete[currentColorIndex]);
                        }
                        else
                        {
                            if (Math.Abs((y - mouseDownPosition.Y)) < Math.Abs((x - mouseDownPosition.X)))
                            {
                                if (mouseDownPosition.X > x)
                                {
                                    DrawLineBelow(x, y, (int)mouseDownPosition.X, (int)mouseDownPosition.Y, colorPallete[currentColorIndex]);
                                }
                                else
                                {
                                    DrawLineBelow((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x, y, colorPallete[currentColorIndex]);
                                }
                            }
                            else
                            {
                                if (mouseDownPosition.Y > y)
                                {
                                    DrawLineAbove(x, y, (int)mouseDownPosition.X, (int)mouseDownPosition.Y, colorPallete[currentColorIndex]);
                                }
                                else
                                {
                                    DrawLineAbove((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x, y, colorPallete[currentColorIndex]);
                                }
                            }
                        }
                        break;
                    }
                case tools.ellipsis:
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                        {
                            //Kreslit kruh
                            int centerX = ((int)mouseDownPosition.X + x) / 2;
                            int centerY = ((int)mouseDownPosition.Y + y) / 2;
                            int radX = centerX - Math.Min((int)mouseDownPosition.X, x);
                            int radY = centerY - Math.Min((int)mouseDownPosition.Y, y);
                            int rad = Math.Min(radX, radY);
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                            {
                                DrawCircle(centerX, centerY, rad, colorPallete[currentColorIndex], true);
                            }
                            else 
                            {
                                DrawCircle(centerX, centerY, rad, colorPallete[currentColorIndex], false);
                            }
                        }
                        else
                        {
                            //Kreslit elipsu
                            int centerX = ((int)mouseDownPosition.X + x) / 2;
                            int centerY = ((int)mouseDownPosition.Y + y) / 2;
                            int radX = centerX - Math.Min((int)mouseDownPosition.X, x);
                            int radY = centerY - Math.Min((int)mouseDownPosition.Y, y);
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                            {
                                DrawEllipse(centerX, centerY, radX, radY, colorPallete[currentColorIndex], true);
                            }
                            else
                            {
                                DrawEllipse(centerX, centerY, radX, radY, colorPallete[currentColorIndex], false);
                            }
                        }
                        break;
                    }
                case tools.rectangle:
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                        {
                            //Kreslit čtverec
                            int xDistance = Math.Abs((int)mouseDownPosition.X - x);
                            int yDistance = Math.Abs((int)mouseDownPosition.Y - y);
                            int dif = Math.Abs(yDistance - xDistance);
                            bool fill;
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                            {
                                fill = true;
                            }
                            else
                            {
                                fill = false;
                            }

                            //Delší stranu je nutné zkrátit o rozdíl, poté se dá použít stejná funkce pro kreslení obdélníků 
                            if (xDistance < yDistance)
                            {
                                if (mouseDownPosition.Y < y)
                                {
                                    DrawRectangle((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x, y - dif, colorPallete[currentColorIndex], fill);
                                }
                                else
                                {
                                    DrawRectangle((int)mouseDownPosition.X, (int)mouseDownPosition.Y - dif, x, y, colorPallete[currentColorIndex], fill);
                                }
                            }
                            else
                            {
                                if (mouseDownPosition.X < x)
                                {
                                    DrawRectangle((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x - dif, y, colorPallete[currentColorIndex], fill);
                                }
                                else
                                {
                                    DrawRectangle((int)mouseDownPosition.X - dif, (int)mouseDownPosition.Y, x, y, colorPallete[currentColorIndex], fill);
                                }
                            }
                        }
                        else
                        {
                            //Kreslit obdélník
                            bool fill;
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                            {
                                fill = true;
                            }
                            else
                            {
                                fill = false;
                            }
                            DrawRectangle((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x, y, colorPallete[currentColorIndex], fill);
                        }
                        break;
                    }
                default: break;
            }
        }

        private void StrokeThicknessSetter(int x, int y, Color color)
        {
            //Při kreslení přímek se musí již navštívené pixely přeskočit, aby nedošlo k nerovnoměrně vybarveným přímkám při velikostech > 1 a alpha < 255
            if (strokeThickness == 1)
            {
                if (alphaBlending == true)
                {
                    Color currentPixelColor = GetPixelColor(x, y, currentBitmap);
                    Color colorMix = ColorMix(color, currentPixelColor);
                    AddPixel(x, y, colorMix, currentBitmap);
                }
                else
                {
                    AddPixel(x, y, color, currentBitmap);
                }
            }
            else
            {
                int size = strokeThickness - 1;
                for (int i = -size; i < size; i++)
                {
                    for (int j = -size; j < size; j++)
                    {
                        // zkontrolovat jestli se pixel vejde do bitmapy
                        if (x + i < width && x + i > -1 && y + j < height && y + j > -1)
                        {
                            Color currentPixelColor = GetPixelColor(x + i, y + j, currentBitmap);
                            Color colorMix = ColorMix(color, currentPixelColor);
                            AddPixel(x + i, y + j, colorMix, currentBitmap);
                        }
                    }
                }
            }
        }

        private void AddPixel(int x, int y, Color color, WriteableBitmap bitmap)
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

                    int colorData = color.A << 24;       // A
                    colorData |= color.R << 16;          // R
                    colorData |= color.G << 8;           // G
                    colorData |= color.B << 0;           // B

                    // Assign the color data to the pixel.
                    *((int*)pBackBuffer) = colorData;

                    bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
                }
            }
            finally
            {
                // Release the back buffer and make it available for display.
                bitmap.Unlock();
            }
        }

        unsafe Color GetPixelColor(int x, int y, WriteableBitmap writeableBitmap)
        {
            Color pix = new Color();
            byte[] colorData = { 0, 0, 0, 0 }; // ARGB
            IntPtr pBackBuffer = writeableBitmap.BackBuffer;
            byte* pBuff = (byte*)pBackBuffer.ToPointer();
            var a = pBuff[4 * x + (y * writeableBitmap.BackBufferStride) + 3];
            var r = pBuff[4 * x + (y * writeableBitmap.BackBufferStride) + 2];
            var g = pBuff[4 * x + (y * writeableBitmap.BackBufferStride) + 1];
            var b = pBuff[4 * x + (y * writeableBitmap.BackBufferStride)];
            pix.A = a;
            pix.R = r;
            pix.G = g;
            pix.B = b;
            return pix;
        }

        public static Color ColorMix(Color foregroundColor, Color backgroundColor)
        {
            byte a = (byte)(255 - ((255 - backgroundColor.A) * (255 - foregroundColor.A) / 255));
            byte r = (byte)((backgroundColor.R * (255 - foregroundColor.A) + foregroundColor.R * foregroundColor.A) / 255);
            byte g = (byte)((backgroundColor.G * (255 - foregroundColor.A) + foregroundColor.G * foregroundColor.A) / 255);
            byte b = (byte)((backgroundColor.B * (255 - foregroundColor.A) + foregroundColor.B * foregroundColor.A) / 255);
            return Color.FromArgb(a, r, g, b);
        }

        private void ColorPicker(int x, int y, int colorIndex)
        {
            colorPallete[colorIndex] = GetPixelColor(x, y, currentBitmap);
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
        }

        //Bresenhaimův algoritmus pro kreslení přímek
        private void DrawLineBelow(int x0, int y0, int x1, int y1, Color color)
        {
            int dx = x1 - x0;
            int dy = y1 - y0;
            int yi = 1;

            if (dy < 0)
            {
                yi = -1;
                dy = -dy;
            }

            int D = (2 * dy) - dx;
            int y = y0;

            for (int x = x0; x < x1; x++)
            {
                StrokeThicknessSetter(x, y, color);

                if (D > 0)
                {
                    y = y + yi;
                    D = D + (2 * (dy - dx));
                }
                else
                {
                    D = D + 2 * dy;
                }
            }
        }

        private void DrawLineAbove(int x0, int y0, int x1, int y1, Color color)
        {
            int dx = x1 - x0;
            int dy = y1 - y0;
            int xi = 1;

            if (dx < 0)
            {
                xi = -1;
                dx = -dx;
            }

            int D = (2 * dx) - dy;
            int x = x0;

            for (int y = y0; y < y1; y++)
            {
                StrokeThicknessSetter(x, y, color);

                if (D > 0)
                {
                    x = x + xi;
                    D = D + (2 * (dx - dy));
                }
                else
                {
                    D = D + 2 * dx;
                }
            }
        }

        private void DrawStraightLine(int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Math.Abs(x1 - x0) + 1;
            int dy = Math.Abs(y1 - y0) + 1;

            //Kroky musí mít rovnoměrné rozdělení
            double ratio = Math.Max(dx, dy) / Math.Min(dx, dy);
            double pixelStep = Math.Round(ratio);

            if (pixelStep > Math.Min(dx, dy))
            {
                pixelStep = Math.Max(width, height);
            }

            int maxDistance = (int)Math.Sqrt((Math.Pow(x0 - x1, 2) + Math.Pow(y0 - y1, 2)));
            int x = x0;
            int y = y0;

            for (int i = 1; i <= maxDistance; i++)
            {
                StrokeThicknessSetter(x, y, color);

                if (Math.Sqrt((Math.Pow(x0 - x, 2) + Math.Pow(y0 - y, 2))) >= maxDistance)
                {
                    break;
                }

                bool isAtStep;

                if (i % pixelStep == 0)
                {
                    isAtStep = true;
                }
                else
                {
                    isAtStep = false;
                }

                if (dx >= dy || isAtStep)
                {
                    if (x0 < x1)
                    {
                        x += 1;
                    }
                    else
                    {
                        x -= 1;
                    }
                }

                if (dy >= dx || isAtStep)
                {
                    if (y0 < y1)
                    {
                        y += 1;
                    }
                    else
                    {
                        y -= 1;
                    }
                }
            }
        }

        //V případě této aplikace je nutné používat Mid-Point algoritmus, protože Bresenhaimův algoritmus nedosahuje při nízkých velikostech kruhu vzhledného výsledku
        private void DrawCircle(int centerX, int centerY, int rad, Color color, bool fill)
        {
            int x = rad, y = 0;

            // When radius is zero only a single point will be printed
            if (rad > 0)
            {
                if (fill)
                {
                    DrawStraightLine(centerX + x, centerY + y, centerX - rad - 1, centerY + y, color);
                }
                else 
                {
                    StrokeThicknessSetter(centerX + x, centerY + y, color);
                    StrokeThicknessSetter(x + centerX - rad, centerY - rad, color);
                    StrokeThicknessSetter(y + centerX, x + centerY, color);
                    StrokeThicknessSetter(-rad + centerX, x + centerY - rad, color);
                }
            }
            else
            {
                StrokeThicknessSetter(centerX, centerY, color);
            }

            // Initialising the value of P
            int P = 1 - rad;
            while (x > y)
            {
                y++;

                // Mid-point is inside or on the perimeter
                if (P <= 0)
                    P = P + 2 * y + 1;

                // Mid-point is outside the perimeter
                else
                {
                    x--;
                    P = P + 2 * y - 2 * x + 1;
                }

                // All the perimeter points have already been printed
                if (x < y)
                    break;

                // Printing the generated point and its reflection in the other octants after translation
                QuadrantPlotter(centerX, centerY, x, y, color, fill);

                // If the generated point is on the line x = y then the perimeter points have already been printed
                if (x != y)
                {
                    if (fill)
                    {
                        DrawStraightLine(y + centerX, x + centerY, -y + centerX - 1, x + centerY, color);
                        DrawStraightLine(y + centerX, -x + centerY, -y + centerX - 1, -x + centerY, color);
                    }
                    else
                    {
                        StrokeThicknessSetter(y + centerX, x + centerY, color);
                        StrokeThicknessSetter(-y + centerX, x + centerY, color);
                        StrokeThicknessSetter(y + centerX, -x + centerY, color);
                        StrokeThicknessSetter(-y + centerX, -x + centerY, color);
                    }
                }
            }
        }
 
        void DrawEllipse(int centerX, int centerY, int radX, int radY, Color color, bool fill){
            int radX2 = radX * radX;
            int radY2 = radY * radY;
            int twoRadX2 = 2 * radX2;
            int twoRadY2 = 2 * radY2;
            int p;
            int x = 0;
            int y = radY;
            int px = 0;
            int py = twoRadX2 * y;

            // Plot the initial point in each quadrant
            QuadrantPlotter(centerX, centerY, x, y, color, fill);

            // Initial decision parameter of region 1
            p = (int)Math.Round(radY2 - (radX2* radY) + (0.25 * radX2));

            // Plotting points of region 1
            while (px<py) {
                x++;
                px += twoRadY2;
                // Checking and updating value of decision parameter based on algorithm
                if (p< 0) {
                   p += radY2 + px;
                }
                else
                {
                    y--;
                    py -= twoRadX2;
                    p += radY2 + px - py;
                }
                QuadrantPlotter(centerX, centerY, x, y, color, fill);
            }

            // Decision parameter of region 2
            p = (int)Math.Round(radY2 * (x + 0.5) * (x + 0.5) + radX2 * (y - 1) * (y - 1) - radX2 * radY2);

            // Plotting points of region 2
            while (y > 0)
            {
                y--;
                py -= twoRadX2;
                // Checking and updating parameter value based on algorithm
                if (p > 0)
                {
                    p += radX2 - py;
                }
                else
                {
                    x++;
                    px += twoRadX2;
                    p += radX2 - py + px;
                }
                QuadrantPlotter(centerX, centerY, x, y, color, fill);
            }
        }

        //Vykreslit symetrické body ve všech kvadrantech pomocí souřadnic
        void QuadrantPlotter(int centerX, int centerY, int x, int y, Color color, bool fill = true)
        {
            if (fill)
            {
                DrawStraightLine(centerX - x, centerY + y, centerX + x + 1, centerY + y, color);
                DrawStraightLine(centerX - x, centerY - y, centerX + x + 1, centerY - y, color);
            }
            else 
            {
                AddPixel(centerX + x, centerY + y, color, currentBitmap);
                AddPixel(centerX - x, centerY + y, color, currentBitmap);
                AddPixel(centerX + x, centerY - y, color, currentBitmap);
                AddPixel(centerX - x, centerY - y, color, currentBitmap);
            }
        }

        private void DrawRectangle(int x0, int y0, int x1, int y1, Color color, bool fill)
        {
            if (y0 < y1)
            {
                for (int y = y0; y < y1; y++)
                {
                    if (fill)
                    {
                        DrawStraightLine(x0, y, x1 + 1, y, color);
                    }
                    else 
                    {
                        StrokeThicknessSetter(x0, y, color);
                        StrokeThicknessSetter(x1, y, color);
                    }
                }
            }
            else
            {
                for (int y = y0; y > y1; y--)
                {
                    if (fill)
                    {
                        DrawStraightLine(x0, y, x1 + 1, y, color);
                    }
                    else
                    {
                        StrokeThicknessSetter(x0, y, color);
                        StrokeThicknessSetter(x1, y, color);
                    }
                }
            }

            if (x0 < x1)
            {
                for (int x = x0; x < x1; x++)
                {
                    if (fill)
                    {
                        DrawStraightLine(x, y0, x, y1 + 1, color);
                    }
                    else
                    {
                        StrokeThicknessSetter(x, y0, color);
                        StrokeThicknessSetter(x, y1, color);
                    }
                }
            }
            else
            {
                for (int x = x0; x > x1; x--)
                {
                    if (fill)
                    {
                        DrawStraightLine(x, y0, x, y1 + 1, color);
                    }
                    else
                    {
                        StrokeThicknessSetter(x, y0, color);
                        StrokeThicknessSetter(x, y1, color);
                    }
                }
            }
            StrokeThicknessSetter(x1, y1, color);
        }

        private void Darken(int x, int y)
        {
            double h;
            double l;
            double s;
            int r;
            int g;
            int b;

            Color currentPixelColor = GetPixelColor(x, y, currentBitmap);
            colorSpaceConvertor.RGBToHLS(currentPixelColor.R, currentPixelColor.G, currentPixelColor.B, out h, out l, out s);
            l -= shadingStep;
            if (l < 0)
            {
                l = 0;
            }
            colorSpaceConvertor.HLSToRGB(h, l, s, out r, out g, out b);
            AddPixel(x, y, Color.FromArgb(currentPixelColor.A, (byte)r, (byte)g, (byte)b), currentBitmap);
        }

        private void Lighten(int x, int y)
        {
            double h;
            double l;
            double s;
            int r;
            int g;
            int b;

            Color currentPixelColor = GetPixelColor(x, y, currentBitmap);
            colorSpaceConvertor.RGBToHLS(currentPixelColor.R, currentPixelColor.G, currentPixelColor.B, out h, out l, out s);
            l += shadingStep;
            if (l > 1)
            {
                l = 1;
            }
            colorSpaceConvertor.HLSToRGB(h, l, s, out r, out g, out b);
            AddPixel(x, y, Color.FromArgb(currentPixelColor.A, (byte)r, (byte)g, (byte)b), currentBitmap);
        }

        private void Dithering(int x, int y, Color color01, Color color02)
        {
            if ((x + y) % 2 == 0)
            {
                AddPixel(x, y, color01, currentBitmap);
            }
            else
            {
                AddPixel(x, y, color02, currentBitmap);
            }
        }

        //V případě této aplikace musí být použit 4-straná verze tohoto algoritmu aby se zábránilo únikům v rozích
        //Může způsobit StackOverflowException při větších velikostech
        private void FloodFill(int x, int y, Color newColor, Color seedColor)
        {
            Color currentColor = GetPixelColor(x, y, currentBitmap);
            if (currentColor != newColor && currentColor == seedColor)
            {
                if (alphaBlending == true)
                {
                    Color colorMix = ColorMix(newColor, currentColor);
                    AddPixel(x, y, colorMix, currentBitmap);
                }
                else
                {
                    AddPixel(x, y, newColor, currentBitmap);
                }

                if (x - 1 > -1)
                {
                    FloodFill(x - 1, y, newColor, seedColor);
                }
                if (x + 1 < width)
                {
                    FloodFill(x + 1, y, newColor, seedColor);
                }
                if (y - 1 > -1)
                {
                    FloodFill(x, y - 1, newColor, seedColor);
                }
                if (y + 1 < height)
                {
                    FloodFill(x, y + 1, newColor, seedColor);
                }
            }
        }

        private void SpecialBucket(int x, int y, int colorIndex, WriteableBitmap writeableBitmap)
        {
            Color seedColor = GetPixelColor(x, y, currentBitmap);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Color currentColor = GetPixelColor(i, j, writeableBitmap);
                    if (currentColor == seedColor)
                    {
                        if (alphaBlending == true)
                        {
                            Color colorMix = ColorMix(colorPallete[colorIndex], currentColor);
                            AddPixel(i, j, colorMix, writeableBitmap);
                        }
                        else
                        {
                            AddPixel(i, j, colorPallete[colorIndex], writeableBitmap);
                        }
                    }
                }
            }
        }

        private void SymmetricDrawing(int x, int y, Color color)
        {
            int mirrorPostion = 0;

            //Chybí převrácení podle osy souměrnosti
            if (System.Windows.Forms.Control.ModifierKeys == Keys.Shift)
            {
                StrokeThicknessSetter(x, y, color);

                //Použít horizontální a vertikální osu 
                if (x > currentBitmap.PixelWidth / 2)
                {
                    mirrorPostion = currentBitmap.PixelWidth - x - 1;
                    StrokeThicknessSetter(mirrorPostion, y, color);
                }
                else
                {

                    int dif = (currentBitmap.PixelWidth / 2) - x;
                    mirrorPostion = (currentBitmap.PixelWidth / 2) + dif - 1;
                    StrokeThicknessSetter(mirrorPostion, y, color);
                }

                if (y > currentBitmap.PixelHeight / 2)
                {
                    mirrorPostion = currentBitmap.PixelHeight - y - 1;
                    StrokeThicknessSetter(x, mirrorPostion, color);
                }
                else
                {
                    int dif = (currentBitmap.PixelHeight / 2) - y;
                    mirrorPostion = (currentBitmap.PixelHeight / 2) + dif - 1;
                    StrokeThicknessSetter(x, mirrorPostion, color);
                }
            }
            else if (System.Windows.Forms.Control.ModifierKeys == Keys.Control)
            {
                StrokeThicknessSetter(x, y, color);

                //Použít horizontální osu 
                if (y > currentBitmap.PixelHeight / 2)
                {
                    mirrorPostion = currentBitmap.PixelHeight - y - 1;
                    StrokeThicknessSetter(x, mirrorPostion, color);
                }
                else
                {
                    int dif = (currentBitmap.PixelHeight / 2) - y;
                    mirrorPostion = (currentBitmap.PixelHeight / 2) + dif - 1;
                    StrokeThicknessSetter(x, mirrorPostion, color);
                }
            }
            else
            {
                //Použít vertikální osu 
                if (x > currentBitmap.PixelWidth / 2)
                {
                    mirrorPostion = currentBitmap.PixelWidth - x - 1;
                    StrokeThicknessSetter(mirrorPostion, y, color);
                }
                else
                {
                    int dif = (currentBitmap.PixelWidth / 2) - x;
                    mirrorPostion = (currentBitmap.PixelWidth / 2) + dif - 1;
                    StrokeThicknessSetter(mirrorPostion, y, color);
                }
            }
            StrokeThicknessSetter(x, y, color);

        }

        private void Eraser(int x, int y, WriteableBitmap writeableBitmap)
        {
            if (strokeThickness == 1)
            {
                byte[] ColorData = { 0, 0, 0, 0 }; // B G R
                Int32Rect rect = new Int32Rect(x, y, 1, 1);
                writeableBitmap.WritePixels(rect, ColorData, 4, 0);
            }
            else
            {
                int size = strokeThickness - 1;
                for (int i = -size; i < size; i++)
                {
                    for (int j = -size; j < size; j++)
                    {
                        // zkontrolovat jestli se pixel vejde do bitmapy
                        if (x + i < width && x + i > -1 && y + j < height && y + j > -1)
                        {
                            byte[] ColorData = { 0, 0, 0, 0 }; // B G R
                            Int32Rect rect = new Int32Rect(x + i, y + j, 1, 1);
                            writeableBitmap.WritePixels(rect, ColorData, 4, 0);
                        }
                    }
                }
            }
        }

        private void Eraser_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.eraser;
            ToolEraser.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolEraser;
        }

        private void Brush_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.brush;
            ToolBrush.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolBrush;
        }

        private void ColorPicker_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.colorPicker;
            ToolColorPicker.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolColorPicker;
        }

        private void SymmetricBrush_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.symmetricBrush;
            ToolSymmetricBrush.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolSymmetricBrush;
        }

        private void Bucket_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.bucket;
            ToolBucket.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolBucket;
        }

        private void SpecialBucket_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.specialBucket;
            ToolSpecialBucket.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolSpecialBucket;
        }

        private void Line_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.line;
            ToolLine.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolLine;
        }

        private void Ellipses_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.ellipsis;
            ToolEllipses.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolEllipses;
        }

        private void Shading_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.shading;
            ToolShading.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolShading;
        }

        private void Rectangle_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.rectangle;
            ToolRectangle.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolRectangle;
        }

        private void Dithering_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.dithering;
            ToolDithering.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolDithering;
        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            currentTool = tools.move;
            ToolMove.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolMove;
        }

        private void Flip_Click(object sender, RoutedEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                for(int i = 0; i < bitmaps.Count; i++)
                {
                    WriteableBitmap newBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
                    Flip(newBitmap, bitmaps[i]);
                    bitmaps[i] = newBitmap;
                    if (i == currentBitmapIndex) 
                    {
                        currentBitmap = newBitmap;
                        bitmaps[currentBitmapIndex] = newBitmap;
                        image.Source = currentBitmap;
                    }
                }
                UpdateImagePreviewButtons();
            }
            else
            {
                WriteableBitmap newBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
                Flip(newBitmap, currentBitmap);
                currentBitmap = newBitmap;
                bitmaps[currentBitmapIndex] = newBitmap;
                image.Source = currentBitmap;
                UpdateImagePreviewButtons();
            }
        }

        private void Flip(WriteableBitmap newBitmap, WriteableBitmap sourceBitmap) 
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                for (int x = 0; x < sourceBitmap.PixelWidth; x++)
                {
                    for (int y = 0; y < sourceBitmap.PixelHeight; y++)
                    {
                        int yp = sourceBitmap.PixelHeight - y - 1;
                        AddPixel(x, yp, GetPixelColor(x, y, sourceBitmap), newBitmap);
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
                        AddPixel(xp, y, GetPixelColor(x, y, sourceBitmap), newBitmap);
                    }
                }
            }
        }

        private void Rotate_Click(object sender, RoutedEventArgs e)
        {


            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
            {
                int newWidth = height;
                height = width;
                width = newWidth;
                for (int i = 0; i < bitmaps.Count; i++)
                {
                    WriteableBitmap newBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
                    RotateAll(newBitmap, bitmaps[i]);
                    bitmaps[i] = newBitmap;
                    if (i == currentBitmapIndex)
                    {
                        currentBitmap = newBitmap;
                        bitmaps[currentBitmapIndex] = newBitmap;
                        image.Width = newBitmap.PixelWidth;
                        image.Height = newBitmap.PixelHeight;
                        image.Source = currentBitmap;
                        paintSurface.Width = newBitmap.PixelWidth;
                        paintSurface.Height = newBitmap.PixelHeight;
                    }
                    UpdateImagePreviewButtons();
                }
            }
            else
            {
                WriteableBitmap newBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
                RotateSingle(newBitmap, currentBitmap);
                currentBitmap = newBitmap;
                bitmaps[currentBitmapIndex] = newBitmap;
                image.Width = newBitmap.PixelWidth;
                image.Height = newBitmap.PixelHeight;
                image.Source = currentBitmap;
                paintSurface.Width = newBitmap.PixelWidth;
                paintSurface.Height = newBitmap.PixelHeight;
                UpdateImagePreviewButtons();
            }
        }

        private void RotateAll(WriteableBitmap newBitmap, WriteableBitmap sourceBitmap) 
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
            {
                for (int x = 0; x < sourceBitmap.PixelWidth; x++)
                {
                    for (int y = 0; y < sourceBitmap.PixelHeight; y++)
                    {
                        AddPixel(sourceBitmap.PixelHeight - y - 1, x, GetPixelColor(x, y, sourceBitmap), newBitmap);
                    }
                }
            }
            else
            {
                for (int x = 0; x < sourceBitmap.PixelWidth; x++)
                {
                    for (int y = 0; y < sourceBitmap.PixelHeight; y++)
                    {
                        AddPixel(y, sourceBitmap.PixelWidth - x - 1, GetPixelColor(x, y, sourceBitmap), newBitmap);
                    }
                }
            }
        }

        private void RotateSingle(WriteableBitmap newBitmap, WriteableBitmap sourceBitmap)
        {
            int widthShift = 0;
            int heightShift = 0;
            CroppedBitmap croppedBitmap;

            //Zvolení posunu zkrácené bitmapy 
            if (width < height)
            {
                croppedBitmap = new CroppedBitmap(currentBitmap, new Int32Rect(0, height / 2 - width / 2, width, height / 2 + width / 2));
                heightShift = (height / 2 - width / 2);
            }
            else if (height < width)
            {
                croppedBitmap = new CroppedBitmap(currentBitmap, new Int32Rect(width / 2 - height / 2, 0, width / 2 + height / 2, height));
                widthShift = (width / 2 - height / 2);
            }
            else 
            {
                croppedBitmap = new CroppedBitmap(currentBitmap, new Int32Rect(0, 0, width, height));
            }

            WriteableBitmap temporaryBitmap = new WriteableBitmap(croppedBitmap);
            int size = Math.Min(temporaryBitmap.PixelWidth, temporaryBitmap.PixelHeight);
            WriteableBitmap rotatedBitmap = new WriteableBitmap(size, size, temporaryBitmap.PixelHeight), 1, 1, PixelFormats.Bgra32, null);

            //Rotace dočasné bitmapy
            for (int x = 0; x < rotatedBitmap.PixelWidth; x++)
            {
                for (int y = 0; y < rotatedBitmap.PixelHeight; y++)
                {
                    //Směr rotace
                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                    {
                        AddPixel(rotatedBitmap.PixelHeight - y - 1, x, GetPixelColor(x, y, temporaryBitmap), rotatedBitmap);
                    }
                    else
                    {
                        AddPixel(y, rotatedBitmap.PixelWidth - x - 1, GetPixelColor(x, y, temporaryBitmap), rotatedBitmap);
                    }
                }
            }

            //Zapsaní otočené bitmapy s případným posunem do nové bitmapy
            for (int x = 0; x < rotatedBitmap.PixelWidth; x++)
            {
                for (int y = 0; y < rotatedBitmap.PixelHeight; y++)
                {
                    AddPixel(x + widthShift, y + heightShift, GetPixelColor(x, y, rotatedBitmap), newBitmap);
                }
            }
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            WindowResize subWindow = new WindowResize();
            subWindow.ShowDialog();

            int croppedWidth;
            int croppedHeight;


            if (subWindow.newWidth != 0 && subWindow.newHeight != 0)
            {
                for (int k = 0; k < bitmaps.Count; k++)
                {
                    WriteableBitmap newBitmap = new WriteableBitmap(subWindow.newWidth, subWindow.newHeight, 1, 1, PixelFormats.Bgra32, null);

                    //Získání pixelů z aktuální bitmapy
                    for (int i = 0; i <= subWindow.newWidth; i++)
                    {
                        for (int j = 0; j <= subWindow.newHeight; j++)
                        {
                            Color color = GetPixelColor(i, j, bitmaps[k]);
                            if (color.A != 0)
                            {
                                //Vytvoření pixelu, který je posunutý v nové bitmapě 
                                //AddPixel(i - leftPixelX, j - topPixelY, color, newBitmap);
                            }
                        }
                    }

                    bitmaps[k] = newBitmap;
                    if (k == currentBitmapIndex)
                    {
                        currentBitmap = newBitmap;
                        image.Source = currentBitmap;
                    }
                }

                width = subWindow.newWidth;
                height = subWindow.newHeight;
                paintSurface.Width = width;
                paintSurface.Height = height;
                image.Width = width;
                image.Height = height;
                if (onionSkinning == true) UpdateOnionSkinning();
                UpdateImagePreviewButtons();
            }
        }

        private void Center_Click(object sender, RoutedEventArgs e)
        {
            Grid_TranslateTransform.X = 0;
            Grid_TranslateTransform.Y = 0;
            Console.WriteLine(grid.ActualWidth + " " + grid.ActualHeight);
        }

        private void Canvas_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            UIElement element = (UIElement)sender;
            Point position = e.GetPosition(element);
            MatrixTransform transform = (MatrixTransform)paintSurface.RenderTransform;
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

            matrix.ScaleAtPrepend(scale, scale, mousePosition.X - paintSurface.Width / 2, mousePosition.Y - paintSurface.Height / 2);
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
                gridDragOffset = new System.Windows.Vector(Grid_TranslateTransform.X, Grid_TranslateTransform.Y);
                grid.CaptureMouse();
            }
        }

        private void Grid_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (grid.IsMouseCaptured)
            {
                System.Windows.Vector offset = Point.Subtract(e.GetPosition(window), gridDragStartPoint);

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
        private void ExportSingle_Click(object sender, RoutedEventArgs e)
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
                            encoder.Frames.Add(BitmapFrame.Create(currentBitmap.Clone()));
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


        private void ExportFull_Click(object sender, RoutedEventArgs e)
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
                            int finalWidth = width;
                            int finalHeight = height * bitmaps.Count();
                            WriteableBitmap finalBitmap = new WriteableBitmap(finalWidth, finalHeight, 1, 1, PixelFormats.Bgra32, null);
                            for (int k = 0; k < bitmaps.Count; k++) 
                            {
                                for (int i = 0; i < width; i++) 
                                {
                                    for (int j = 0; j < height; j++)
                                    {
                                        Color color = GetPixelColor(i, j, bitmaps[k]);
                                        AddPixel(i, j + (k * height), color, finalBitmap);
                                    }
                                }
                            }

                            PngBitmapEncoder encoder = new PngBitmapEncoder();
                            encoder.Frames.Add(BitmapFrame.Create(finalBitmap.Clone()));
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
                    var filePath = dialog.FileName;
                    BitmapImage thisIsABitmapImage = new BitmapImage(new Uri(filePath));
                    WriteableBitmap newBitmap = new WriteableBitmap(thisIsABitmapImage);
                    currentBitmap = newBitmap;
                    image.Source = currentBitmap;
                    bitmaps[0] = currentBitmap;
                    //Clear nesmí být použito protože to potom vytváří chybu v OnTimedEvent
                    for (int i = 1; i < bitmaps.Count; i++)
                    {
                        bitmaps.RemoveAt(i);
                    }
                    
                    width = newBitmap.PixelWidth;
                    height = newBitmap.PixelHeight;
                    paintSurface.Width = width;
                    paintSurface.Height = height;
                    image.Width = width;
                    image.Height = height;
                    previewBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
                    previewImage.Source = previewBitmap;
                    previewImage.Width = width;
                    previewImage.Height = height;
                    if (onionSkinning == true) UpdateOnionSkinning();
                    UpdateImagePreviewButtons();
                    currentBitmapIndex = 0;
                    LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
                    LabelPosition.Content = "[" + width + ":" + height + "] " + mousePosition.X + ":" + mousePosition.Y;
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
                newWriteableBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
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
            UpdateImagePreviewButtons();
            if (onionSkinning == true) UpdateOnionSkinning();
        }

        private void DeleteImage_Click(object sender, RoutedEventArgs e)
        {
            bitmaps.RemoveAt(currentBitmapIndex);
            if (bitmaps.Count == 0)
            {
                WriteableBitmap newWriteableBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
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
            UpdateImagePreviewButtons();
            if (onionSkinning == true) UpdateOnionSkinning();
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

        private void UpdateImagePreviewButtons() 
        {
            List<System.Windows.Controls.Button> previewButtons = ImagePreviews.Children.OfType<System.Windows.Controls.Button>().ToList();
            foreach (System.Windows.Controls.Button btn in previewButtons)
            {
                ImagePreviews.Children.Remove(btn);
            }

            for (int i = 0; i < bitmaps.Count; i++)
            {
                System.Windows.Controls.Button newButton = new System.Windows.Controls.Button();
                var brush = new ImageBrush();
                brush.ImageSource = bitmaps[i];
                newButton.Content = new Image
                {
                    Source = bitmaps[i],
                    VerticalAlignment = VerticalAlignment.Center,
                    Stretch = Stretch.Uniform,
                    Height = 140,
                    Width = 140
                };
                newButton.Width = 140;
                newButton.Height = 140;
                newButton.Name = "ImagePreview" + i.ToString();
                newButton.AddHandler(System.Windows.Controls.Button.ClickEvent, new RoutedEventHandler(PreviewButton_Click));
                ImagePreviews.Children.Add(newButton);
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            string buttonName = ((System.Windows.Controls.Button)sender).Name;
            int index = int.Parse(buttonName.Replace("ImagePreview", ""));
            currentBitmapIndex = index;
            UpdateCurrentBitmap();
        }

        private void Previous_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmapIndex - 1 > -1)
            {
                currentBitmapIndex -= 1;
                UpdateCurrentBitmap();
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            if (currentBitmapIndex + 1 < bitmaps.Count)
            {
                currentBitmapIndex += 1;
                UpdateCurrentBitmap();
            }
        }

        private void UpdateCurrentBitmap()
        {
            currentBitmap = bitmaps[currentBitmapIndex];
            image.Source = currentBitmap;
            LabelImages.Content = bitmaps.Count.ToString() + ":" + (currentBitmapIndex + 1).ToString();
            currentAnimationIndex = currentBitmapIndex;
            animationPreview.Source = bitmaps[currentAnimationIndex];
            if (onionSkinning == true) UpdateOnionSkinning();
        }

        private void AlphaBlending_Checked(object sender, RoutedEventArgs e)
        {
            alphaBlending = true;
        }

        private void AlphaBlending_Unchecked(object sender, RoutedEventArgs e)
        {
            alphaBlending = false;
        }

        private void UpdateOnionSkinning()
        {
            RemoveOnionSkinning();
            if (currentBitmapIndex > 0)
            {
                Image previousBitmap = new Image();
                previousBitmap.Source = bitmaps[currentBitmapIndex - 1];
                previousBitmap.Opacity = 0.25f;
                previousBitmap.Width = currentBitmap.PixelWidth;
                previousBitmap.Height = currentBitmap.PixelHeight;
                RenderOptions.SetBitmapScalingMode(previousBitmap, BitmapScalingMode.NearestNeighbor);
                paintSurface.Children.Add(previousBitmap);
            }

            if (currentBitmapIndex < bitmaps.Count - 1)
            {
                Image nextBitmap = new Image();
                nextBitmap.Source = bitmaps[currentBitmapIndex + 1];
                nextBitmap.Opacity = 0.25f;
                nextBitmap.Width = currentBitmap.PixelWidth;
                nextBitmap.Height = currentBitmap.PixelHeight;
                RenderOptions.SetBitmapScalingMode(nextBitmap, BitmapScalingMode.NearestNeighbor);
                paintSurface.Children.Add(nextBitmap);
            }
        }

        private void RemoveOnionSkinning()
        {
            List<Image> children = paintSurface.Children.OfType<Image>().ToList();
            foreach (Image child in children)
            {
                if (child != image)
                {
                    paintSurface.Children.Remove(child);
                }
            }
        }

        private void CropToFit_Click(object sender, RoutedEventArgs e)
        {
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

                //Projít ze dolu a doprava 
                for (int i = 0; i < width; i++)
                {
                    for (int j = 0; j < height; j++)
                    {
                        Color color = GetPixelColor(i, j, bitmap);
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
                for (int i = width; i >= 0; i--)
                {
                    for (int j = height; j >= 0; j--)
                    {
                        Color color = GetPixelColor(i, j, bitmap);
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
            }

            int newWidth = rightPixelX - leftPixelX + 1;
            int newHeight =  downPixelY - topPixelY + 1;
            if (newWidth > 0 && newHeight > 0)
            {
                Console.WriteLine(newWidth);
                Console.WriteLine(newHeight);
                for (int k = 0; k < bitmaps.Count; k++)
                {
                    WriteableBitmap newBitmap = new WriteableBitmap(newWidth, newHeight, 1, 1, PixelFormats.Bgra32, null);

                    //Získání pixelů z aktuální bitmapy
                    for (int i = leftPixelX; i <= rightPixelX; i++)
                    {
                        for (int j = topPixelY; j <= downPixelY; j++)
                        {
                            Color color = GetPixelColor(i, j, bitmaps[k]);
                            if (color.A != 0)
                            {
                                //Vytvoření pixelu, který je posunutý v nové bitmapě 
                                AddPixel(i - leftPixelX, j - topPixelY, color, newBitmap);
                            }
                        }
                    }

                    bitmaps[k] = newBitmap;
                    if (k == currentBitmapIndex)
                    {
                        currentBitmap = newBitmap;
                        image.Source = currentBitmap;
                    }
                }

                width = newWidth;
                height = newHeight;
                paintSurface.Width = width;
                paintSurface.Height = height;
                image.Width = width;
                image.Height = height;
                previewBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
                previewImage.Source = previewBitmap;
                previewImage.Width = width;
                previewImage.Height = height;
                if (onionSkinning == true) UpdateOnionSkinning();
                UpdateImagePreviewButtons();
            }
        }

        private void CenterAlligment_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SwapColors_Click(object sender, RoutedEventArgs e)
        {
            Color tempColor = colorPallete[0];
            colorPallete[0] = colorPallete[1];
            colorPallete[1] = tempColor;

            SolidColorBrush brush0 = new SolidColorBrush();
            brush0.Color = colorPallete[0];
            ColorSelector0.Background = brush0;

            SolidColorBrush brush1 = new SolidColorBrush();
            brush1.Color = colorPallete[1];
            ColorSelector1.Background = brush1;
        }

        private void OnionSkinning_Checked(object sender, RoutedEventArgs e)
        {
            onionSkinning = true;
            UpdateOnionSkinning();
        }

        private void OnionSkinning_Unchecked(object sender, RoutedEventArgs e)
        {
            onionSkinning = false;
            RemoveOnionSkinning();
        }

        private void Additive_Checked(object sender, RoutedEventArgs e)
        {
            additive = true;
        }

        private void Additive_Unchecked(object sender, RoutedEventArgs e)
        {
            additive = false;
        }
    }
}