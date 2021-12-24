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

namespace BakalarskaPrace
{
    public partial class MainWindow : Window
    {

        Point currentPoint = new Point();
        int colorPalleteSize = 2;
        Color[] colorPallete;
        int strokeThickness = 1;
        byte alpha = 255;
        enum tools {brush, erasor};
        tools currentTool = tools.brush;

        public MainWindow()
        {
            colorPallete = new Color[colorPalleteSize];
            colorPallete[0] = Color.FromArgb(alpha, 0, 0, 0);         //Primární barva
            colorPallete[1] = Color.FromArgb(alpha, 255, 255, 255);   //Sekundární barva
            InitializeComponent();
        }

        private void Canvas_MouseDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                currentPoint = e.GetPosition(paintSurface);
            switch (currentTool)
            {
                case tools.brush:
                    {
                        
                        break;
                    }
                case tools.erasor:
                    {
                        
                        break;
                    }
                default: break;
            }
        }

        private void Canvas_MouseMove_1(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
            {
                SolidColorBrush brush = new SolidColorBrush();
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    brush.Color = colorPallete[0];
                }
                else
                {
                    brush.Color = colorPallete[1];
                }
                Line line = new Line();

                line.Stroke = SystemColors.WindowFrameBrush;
                line.X1 = currentPoint.X;
                line.Y1 = currentPoint.Y;
                line.X2 = e.GetPosition(paintSurface).X;
                line.Y2 = e.GetPosition(paintSurface).Y;

                line.Stroke = brush;
                line.StrokeThickness = strokeThickness;
                currentPoint = e.GetPosition(paintSurface);

                paintSurface.Children.Add(line);
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

        private bool dragStarted = false;

        private void BrushSize_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            /*DoWork(((Slider)sender).Value);
            if (!dragStarted) strokeThickness = (int)e.Value;
            this.dragStarted = false;*/
        }

        private void BrushSize_DragStarted(object sender, DragStartedEventArgs e)
        {
            //this.dragStarted = true;
        }

        private void BrushSize_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            strokeThickness = (int)e.NewValue;
        }

        private void Transparency_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            /*DoWork(((Slider)sender).Value);
            if (!dragStarted) strokeThickness = (int)e.Value;
            this.dragStarted = false;*/
        }

        private void Transparency_DragStarted(object sender, DragStartedEventArgs e)
        {
            //this.dragStarted = true;
        }

        private void Transparency_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            alpha = (byte)e.NewValue;
            for (int i = 0; i < colorPallete.Length; i++) 
            {
                colorPallete[i].A = alpha;
            }
        }
    }
}