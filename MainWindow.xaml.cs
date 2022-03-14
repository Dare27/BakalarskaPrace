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
using ImageMagick;

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
        
        int strokeThickness = 1;
        byte alpha = 255;
        bool alphaBlending = true;
        bool additive = false;
        enum toolSelection { brush, eraser, symmetricBrush, colorPicker, bucket, specialBucket, line, ellipsis, shading, rectangle, dithering, move };
        toolSelection currentTool = toolSelection.brush;
        const double scaleRate = 1.1;
        Point gridDragStartPoint;
        System.Windows.Vector gridDragOffset;
        int width;
        int height;
        double currentScale = 1.0;
        Vector2 mousePosition = new Vector2(0, 0);
        Vector2 previousMousePosition = new Vector2(0, 0);
        Vector2 previewMousePosition = new Vector2(0, 0);
        List<Vector2> previewMousePoints = new List<Vector2>();

        Vector2 mouseDownPosition = new Vector2(-1, -1);
        List<Vector2> visitedPoints = new List<Vector2>();

        int currentColorIndex = 0;
        
        bool onionSkinning;
        System.Windows.Controls.Button lastToolButton;

        Color defaultPreviewColor;
        WriteableBitmap previewBitmap;

        private System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
        int timerInterval = 1000;
        int currentAnimationIndex;
        int currentFPSTarget = 12;
        ImageManipulation imageManipulation;
        Geometry geometry;
        Transform transform;
        Tools tools;

        public MainWindow()
        {
            colorPallete = new Color[colorPalleteSize];
            colorPallete[0] = Color.FromArgb(alpha, 0, 0, 0);         //Primární barva
            colorPallete[1] = Color.FromArgb(alpha, 255, 255, 255);   //Sekundární barva
            defaultPreviewColor = Color.FromArgb(255, 178, 213, 226);
            imageManipulation = new ImageManipulation();
            geometry = new Geometry();
            transform = new Transform();
            tools = new Tools();

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
            previewImage.Width = width;
            previewImage.Height = height;

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
                    case toolSelection.brush:
                        {
                            StrokeThicknessSetter(x, y, colorPallete[colorIndex], currentBitmap);
                            break;
                        }
                    case toolSelection.symmetricBrush:
                        {
                            List<Vector2> points = tools.SymmetricDrawing(x, y, colorPallete[colorIndex], currentBitmap);
                            foreach (Vector2 point in points) 
                            {
                                StrokeThicknessSetter((int)point.X, (int)point.Y, colorPallete[colorIndex], currentBitmap);
                            }
                            break;
                        }
                    case toolSelection.eraser:
                        {
                            imageManipulation.Eraser(x, y, currentBitmap, strokeThickness, width, height);
                            break;
                        }
                    case toolSelection.colorPicker:
                        {
                            ColorPicker(x, y, colorIndex);
                            break;
                        }
                    case toolSelection.bucket:
                        {
                            Color seedColor = imageManipulation.GetPixelColor(x, y, currentBitmap);
                            tools.FloodFill(x, y, colorPallete[colorIndex], seedColor, currentBitmap, alphaBlending, width, height);
                            break;
                        }
                    case toolSelection.specialBucket:
                        {

                            Color seedColor = imageManipulation.GetPixelColor(x, y, currentBitmap);
                            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                            {
                                foreach (WriteableBitmap writeableBitmap in bitmaps) 
                                {
                                    tools.SpecialBucket(x, y, currentBitmap, colorPallete[colorIndex], seedColor, alphaBlending, width, height);
                                }
                            }
                            else
                            {
                                tools.SpecialBucket(x, y, currentBitmap, colorPallete[colorIndex], seedColor, alphaBlending, width, height);
                            }
                            break;
                        }
                    case toolSelection.line:
                        {
                            mouseDownPosition = new Vector2(x, y);
                            currentColorIndex = colorIndex;
                            break;
                        }
                    case toolSelection.ellipsis:
                        {
                            mouseDownPosition = new Vector2(x, y);
                            currentColorIndex = colorIndex;
                            break;
                        }
                    case toolSelection.rectangle:
                        {
                            mouseDownPosition = new Vector2(x, y);
                            currentColorIndex = colorIndex;
                            break;
                        }
                    case toolSelection.dithering:
                        {
                            tools.Dithering(x, y, colorPallete[0], colorPallete[1], currentBitmap);
                            break;
                        }
                    case toolSelection.shading:
                        {
                            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                            {
                                //Zesvětlení
                                tools.Lighten(x, y, currentBitmap);
                            }
                            else
                            {
                                //Ztmavení 
                                tools.Darken(x, y, currentBitmap);
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
                    case toolSelection.brush:
                        {
                            StrokeThicknessSetter(x, y, colorPallete[colorIndex], currentBitmap);
                            break;
                        }
                    case toolSelection.symmetricBrush:
                        {
                            List<Vector2> points = tools.SymmetricDrawing(x, y, colorPallete[colorIndex], currentBitmap);
                            foreach (Vector2 point in points)
                            {
                                StrokeThicknessSetter((int)point.X, (int)point.Y, colorPallete[colorIndex], currentBitmap);
                            }
                            break;
                        }
                    case toolSelection.eraser:
                        {
                            imageManipulation.Eraser(x, y, currentBitmap, strokeThickness, width, height);
                            break;
                        }
                    case toolSelection.colorPicker:
                        {
                            ColorPicker(x, y, colorIndex);
                            break;
                        }
                    case toolSelection.bucket:
                        {
                            Color seedColor = imageManipulation.GetPixelColor(x, y, currentBitmap);
                            tools.FloodFill(x, y, colorPallete[colorIndex], seedColor, currentBitmap, alphaBlending, width, height);
                            break;
                        }
                    case toolSelection.specialBucket:
                        {

                            Color seedColor = imageManipulation.GetPixelColor(x, y, currentBitmap);
                            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                            {
                                foreach (WriteableBitmap writeableBitmap in bitmaps)
                                {
                                    tools.SpecialBucket(x, y, currentBitmap, colorPallete[colorIndex], seedColor, alphaBlending, width, height);
                                }
                            }
                            else
                            {
                                tools.SpecialBucket(x, y, currentBitmap, colorPallete[colorIndex], seedColor, alphaBlending, width, height);
                            }
                            break;
                        }
                    case toolSelection.dithering:
                        {
                            tools.Dithering(x, y, colorPallete[0], colorPallete[1], currentBitmap);
                            break;
                        }
                    case toolSelection.shading:
                        {
                            if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                            {
                                //Zesvětlení
                                tools.Lighten(x, y, currentBitmap);
                            }
                            else
                            {
                                //Ztmavení 
                                tools.Darken(x, y, currentBitmap);
                            }
                            break;
                        }
                    default: break;
                }
            }



           if (x >= 0 && y >= 0 && x < width && y < height && (previewMousePosition.X != x || previewMousePosition.Y != y))
           {
                foreach (Vector2 point in previewMousePoints)
                {
                    imageManipulation.Eraser((int)point.X, (int)point.Y, previewBitmap, strokeThickness, width, height);
                }

                switch (currentTool)
                {
                    case toolSelection.line:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                if ((int)mouseDownPosition.X != -1 && (int)mouseDownPosition.Y != -1)
                                {
                                    List<Vector2> points = new List<Vector2>();
                                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                                    {
                                        points = geometry.DrawStraightLine((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x, y, width, height);
                                    }
                                    else
                                    {
                                        points = geometry.DrawLine(x, y, (int)mouseDownPosition.X, (int)mouseDownPosition.Y);
                                    }

                                    foreach (Vector2 point in points)
                                    {
                                        previewMousePoints.Add(point);
                                        imageManipulation.AddPixel((int)point.X, (int)point.Y, defaultPreviewColor, previewBitmap);
                                    }
                                }
                            }
                            else 
                            {
                                previewMousePosition = new Vector2(x, y);
                                previewMousePoints.Add(previewMousePosition);
                                imageManipulation.AddPixel(x, y, defaultPreviewColor, previewBitmap);
                            }
                            break;
                        }
                    case toolSelection.ellipsis:
                        {
                            
                            break;
                        }
                    case toolSelection.rectangle:
                        {
                            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
                            {
                                if ((int)mouseDownPosition.X != -1 && (int)mouseDownPosition.Y != -1)
                                {
                                    bool fill;
                                    if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                                    {
                                        fill = true;
                                    }
                                    else
                                    {
                                        fill = false;
                                    }

                                    List<Vector2> points = new List<Vector2>();
                                    if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                                    {

                                    }
                                    else
                                    {
                                        //Kreslit obdélník
                                        points = geometry.DrawRectangle((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x, y, fill);
                                    }

                                    foreach (Vector2 point in points)
                                    {
                                        previewMousePoints.Add(point);
                                        imageManipulation.AddPixel((int)point.X, (int)point.Y, defaultPreviewColor, previewBitmap);
                                    }
                                }
                            }
                            else
                            {
                                previewMousePosition = new Vector2(x, y);
                                previewMousePoints.Add(previewMousePosition);
                                imageManipulation.AddPixel(x, y, defaultPreviewColor, previewBitmap);
                            }
                            break;
                        }
                    default:
                        previewMousePosition = new Vector2(x, y);
                        previewMousePoints.Add(previewMousePosition);
                        imageManipulation.AddPixel(x, y, defaultPreviewColor, previewBitmap);
                        break;
                }
            }
        }

        private unsafe void Image_MouseUp(object sender, System.Windows.Input.MouseEventArgs e)
        {
            int x = (int)e.GetPosition(image).X;
            int y = (int)e.GetPosition(image).Y;

            mousePosition = new Vector2(x, y);
            previousMousePosition = new Vector2(-1, -1);
            

            visitedPoints = new List<Vector2>();

            switch (currentTool)
            {
                case toolSelection.line:
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                        {
                            List<Vector2> points = geometry.DrawStraightLine((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x, y, width, height);
                            foreach (Vector2 point in points)
                            {
                                StrokeThicknessSetter((int)point.X, (int)point.Y, colorPallete[currentColorIndex], currentBitmap);
                            }
                        }
                        else
                        {
                            List<Vector2> points = geometry.DrawLine(x, y, (int)mouseDownPosition.X, (int)mouseDownPosition.Y);
                            foreach (Vector2 point in points)
                            {
                                StrokeThicknessSetter((int)point.X, (int)point.Y, colorPallete[currentColorIndex], currentBitmap);
                            }
                        }
                        break;
                    }
                case toolSelection.ellipsis:
                    {
                        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                        {
                            //Kreslit kruh
                            int centerX = ((int)mouseDownPosition.X + x) / 2;
                            int centerY = ((int)mouseDownPosition.Y + y) / 2;
                            int radX = centerX - Math.Min((int)mouseDownPosition.X, x);
                            int radY = centerY - Math.Min((int)mouseDownPosition.Y, y);
                            int rad = Math.Min(radX, radY);
                            List<Vector2> points;

                            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                            {
                                points = geometry.DrawCircle(centerX, centerY, rad, true);
                            }
                            else 
                            {
                                points = geometry.DrawCircle(centerX, centerY, rad, false);
                            }

                            foreach (Vector2 point in points)
                            {
                                StrokeThicknessSetter((int)point.X, (int)point.Y, colorPallete[currentColorIndex], currentBitmap);
                            }
                        }
                        else
                        {
                            //Kreslit elipsu
                            int centerX = ((int)mouseDownPosition.X + x) / 2;
                            int centerY = ((int)mouseDownPosition.Y + y) / 2;
                            int radX = centerX - Math.Min((int)mouseDownPosition.X, x);
                            int radY = centerY - Math.Min((int)mouseDownPosition.Y, y);
                            List<Vector2> points;
                            if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                            {
                                points = geometry.DrawEllipse(centerX, centerY, radX, radY, true);
                            }
                            else
                            {
                                points = geometry.DrawEllipse(centerX, centerY, radX, radY, false);
                            }
                            foreach (Vector2 point in points)
                            {
                                StrokeThicknessSetter((int)point.X, (int)point.Y, colorPallete[currentColorIndex], currentBitmap);
                            }
                        }
                        break;
                    }
                case toolSelection.rectangle:
                    {
                        bool fill;
                        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0)
                        {
                            fill = true;
                        }
                        else
                        {
                            fill = false;
                        }

                        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0)
                        {
                            //Kreslit čtverec
                            int xDistance = Math.Abs((int)mouseDownPosition.X - x);
                            int yDistance = Math.Abs((int)mouseDownPosition.Y - y);
                            int dif = Math.Abs(yDistance - xDistance);

                            //Delší stranu je nutné zkrátit o rozdíl, poté se dá použít stejná funkce pro kreslení obdélníků 
                            List<Vector2> points;
                            if (xDistance < yDistance)
                            {
                                if (mouseDownPosition.Y < y)
                                {
                                    points = geometry.DrawRectangle((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x, y - dif, fill);
                                }
                                else
                                {
                                    points = geometry.DrawRectangle((int)mouseDownPosition.X, (int)mouseDownPosition.Y - dif, x, y, fill);
                                }
                            }
                            else
                            {
                                if (mouseDownPosition.X < x)
                                {
                                    points = geometry.DrawRectangle((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x - dif, y, fill);
                                }
                                else
                                {
                                    points = geometry.DrawRectangle((int)mouseDownPosition.X - dif, (int)mouseDownPosition.Y, x, y, fill);
                                }
                            }
                            foreach (Vector2 point in points)
                            {
                                StrokeThicknessSetter((int)point.X, (int)point.Y, colorPallete[currentColorIndex], currentBitmap);
                            }
                        }
                        else
                        {
                            //Kreslit obdélník
                            List<Vector2> points = geometry.DrawRectangle((int)mouseDownPosition.X, (int)mouseDownPosition.Y, x, y, fill);
                            foreach (Vector2 point in points) 
                            {
                                StrokeThicknessSetter((int)point.X, (int)point.Y, colorPallete[currentColorIndex], currentBitmap);
                            }
                        }
                        break;
                    }
                default: break;
            }

            mouseDownPosition = new Vector2(-1, -1);
        }

        private void StrokeThicknessSetter(int x, int y, Color color, WriteableBitmap bitmap, List<Vector2> points = null)
        {
            //Při kreslení přímek se musí již navštívené pixely přeskočit, aby nedošlo k nerovnoměrně vybarveným přímkám při velikostech > 1 a alpha < 255
            if (points == null) 
            {
                points = new List<Vector2>();
            }

            int size = strokeThickness / 2;
            int isOdd = 0;

            if (strokeThickness % 2 != 0)
            {
                isOdd = 1;
            }
            
            for (int i = -size; i < size + isOdd; i++)
            {
                for (int j = -size; j < size + isOdd; j++)
                {
                    // zkontrolovat jestli se pixel vejde do bitmapy
                    if (x + i < width && x + i > -1 && y + j < height && y + j > -1)
                    {
                        if (!visitedPoints.Contains(new Vector2(x + i, y + j)))
                        {
                            visitedPoints.Add(new Vector2(x + i, y + j));
                            points.Add(new Vector2(x + i, y + j));
                        }
                    }
                }
            }

            foreach (var point in points) 
            {
                if (alphaBlending == true)
                {
                    Color currentPixelColor = imageManipulation.GetPixelColor((int)point.X, (int)point.Y, bitmap);
                    Color colorMix = imageManipulation.ColorMix(color, currentPixelColor);
                    imageManipulation.AddPixel((int)point.X, (int)point.Y, colorMix, bitmap);
                }
                else
                {
                    imageManipulation.AddPixel((int)point.X, (int)point.Y, color, bitmap);
                }
            }
        }

        private void ColorPicker(int x, int y, int colorIndex)
        {
            colorPallete[colorIndex] = imageManipulation.GetPixelColor(x, y, currentBitmap);
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

        private void Eraser_Click(object sender, RoutedEventArgs e)
        {
            currentTool = toolSelection.eraser;
            ToolEraser.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolEraser;
        }

        private void Brush_Click(object sender, RoutedEventArgs e)
        {
            currentTool = toolSelection.brush;
            ToolBrush.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolBrush;
        }

        private void ColorPicker_Click(object sender, RoutedEventArgs e)
        {
            currentTool = toolSelection.colorPicker;
            ToolColorPicker.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolColorPicker;
        }

        private void SymmetricBrush_Click(object sender, RoutedEventArgs e)
        {
            currentTool = toolSelection.symmetricBrush;
            ToolSymmetricBrush.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolSymmetricBrush;
        }

        private void Bucket_Click(object sender, RoutedEventArgs e)
        {
            currentTool = toolSelection.bucket;
            ToolBucket.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolBucket;
        }

        private void SpecialBucket_Click(object sender, RoutedEventArgs e)
        {
            currentTool = toolSelection.specialBucket;
            ToolSpecialBucket.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolSpecialBucket;
        }

        private void Line_Click(object sender, RoutedEventArgs e)
        {
            currentTool = toolSelection.line;
            ToolLine.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolLine;
        }

        private void Ellipses_Click(object sender, RoutedEventArgs e)
        {
            currentTool = toolSelection.ellipsis;
            ToolEllipses.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolEllipses;
        }

        private void Shading_Click(object sender, RoutedEventArgs e)
        {
            currentTool = toolSelection.shading;
            ToolShading.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolShading;
        }

        private void Rectangle_Click(object sender, RoutedEventArgs e)
        {
            currentTool = toolSelection.rectangle;
            ToolRectangle.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolRectangle;
        }

        private void Dithering_Click(object sender, RoutedEventArgs e)
        {
            currentTool = toolSelection.dithering;
            ToolDithering.IsEnabled = false;
            if (lastToolButton != null)
            {
                lastToolButton.IsEnabled = true;
            }
            lastToolButton = ToolDithering;
        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            currentTool = toolSelection.move;
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
                    transform.Flip(newBitmap, bitmaps[i]);
                    bitmaps[i] = newBitmap;
                    if (i == currentBitmapIndex) 
                    {
                        currentBitmap = newBitmap;
                        bitmaps[currentBitmapIndex] = newBitmap;
                        image.Source = currentBitmap;
                    }
                }
            }
            else
            {
                WriteableBitmap newBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
                transform.Flip(newBitmap, currentBitmap);
                currentBitmap = newBitmap;
                bitmaps[currentBitmapIndex] = newBitmap;
                image.Source = currentBitmap;
            }

            UpdateImagePreviewButtons();
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
                    transform.RotateAnimation(newBitmap, bitmaps[i]);
                    bitmaps[i] = newBitmap;
                    if (i == currentBitmapIndex)
                    {
                        currentBitmap = newBitmap;
                    }
                }
            }
            else
            {
                WriteableBitmap newBitmap = new WriteableBitmap(width, height, 1, 1, PixelFormats.Bgra32, null);
                transform.RotateImage(newBitmap, currentBitmap, width, height);
                currentBitmap = newBitmap;
            }
            bitmaps[currentBitmapIndex] = currentBitmap;
            image.Width = currentBitmap.PixelWidth;
            image.Height = currentBitmap.PixelHeight;
            image.Source = currentBitmap;
            paintSurface.Width = currentBitmap.PixelWidth;
            paintSurface.Height = currentBitmap.PixelHeight;
            UpdateImagePreviewButtons();
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
                for (int i = width; i >= 0; i--)
                {
                    for (int j = height; j >= 0; j--)
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
            }

            int newWidth = rightPixelX - leftPixelX + 1;
            int newHeight = downPixelY - topPixelY + 1;
            if (newWidth > 0 && newHeight > 0)
            {
                for (int k = 0; k < bitmaps.Count; k++)
                {
                    WriteableBitmap newBitmap = new WriteableBitmap(newWidth, newHeight, 1, 1, PixelFormats.Bgra32, null);

                    //Získání pixelů z aktuální bitmapy
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
                            Color color = imageManipulation.GetPixelColor(i, j, bitmaps[k]);
                            if (color.A != 0)
                            {
                                //Vytvoření pixelu, který je posunutý v nové bitmapě 
                                //imageManipulation.AddPixel(i - leftPixelX, j - topPixelY, color, newBitmap);
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
            FileManagement fileManagement = new FileManagement();
            fileManagement.ExportPNG(currentBitmap);
        }

        private void ExportFull_Click(object sender, RoutedEventArgs e)
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
                        Color color = imageManipulation.GetPixelColor(i, j, bitmaps[k]);
                        imageManipulation.AddPixel(i, j + (k * height), color, finalBitmap);
                    }
                }
            }
            FileManagement fileManagement = new FileManagement();
            fileManagement.ExportPNG(finalBitmap);
        }

        private void ExportGif_CLick(object sender, RoutedEventArgs e)
        {
            FileManagement fileManagement = new FileManagement();
            fileManagement.ExportGif(bitmaps, timerInterval);
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
                    BitmapImage bitmapImage = new BitmapImage(new Uri(filePath));
                    WriteableBitmap newBitmap = new WriteableBitmap(bitmapImage);
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