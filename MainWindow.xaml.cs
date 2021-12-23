using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Forms;
using System.Windows.Controls.Primitives;

namespace BakalarskaPrace
{
    public partial class MainWindow : Window
    {

        Point currentPoint = new Point();
        Color primaryColor = new Color();
        Color secondaryColor = new Color();
        int strokeThickness = 1;

        public MainWindow()
        {
            primaryColor = Color.FromArgb(255, 0, 0, 0);
            secondaryColor = Color.FromArgb(255, 255, 0, 0);
            InitializeComponent();
        }

        private void Canvas_MouseDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                currentPoint = e.GetPosition(this);
        }

        private void Canvas_MouseMove_1(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
            {
                SolidColorBrush brush = new SolidColorBrush();
                if (e.LeftButton == MouseButtonState.Pressed)
                {
                    brush.Color = primaryColor;
                }
                else 
                {
                    brush.Color = secondaryColor;
                }
                Line line = new Line();

                line.Stroke = SystemColors.WindowFrameBrush;
                line.X1 = currentPoint.X;
                line.Y1 = currentPoint.Y;
                line.X2 = e.GetPosition(this).X;
                line.Y2 = e.GetPosition(this).Y;
                
                line.Stroke = brush;
                line.StrokeThickness = strokeThickness;
                currentPoint = e.GetPosition(this);

                paintSurface.Children.Add(line);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) 
            {
                System.Drawing.Color color = colorDialog.Color;
                primaryColor = System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
            }
        }

        private bool dragStarted = false;

        private void Slider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            /*DoWork(((Slider)sender).Value);
            if (!dragStarted) strokeThickness = (int)e.Value;
            this.dragStarted = false;*/
        }

        private void Slider_DragStarted(object sender, DragStartedEventArgs e)
        {
            //this.dragStarted = true;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            strokeThickness = (int)e.NewValue;
        }
    }
}