using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BakalarskaPrace
{
    internal class ControlCreator
    {
        public Button LayerPreviewButton(int index, int currentLayerIndex, RoutedEventHandler routedEventHandler) 
        {
            Button newButton = new Button();
            newButton.Content = index + 1;
            newButton.IsEnabled = (currentLayerIndex == index) ? false : true;
            newButton.Width = 200;
            newButton.Height = 24;
            newButton.Name = "LayerPreview" + index.ToString();
            newButton.HorizontalContentAlignment = HorizontalAlignment.Left;
            newButton.Margin = new Thickness(2, 1, 2, 1);
            //newButton.SetResourceReference(System.Windows.Controls.Control.StyleProperty, "AnimationButton");
            newButton.AddHandler(System.Windows.Controls.Button.ClickEvent, routedEventHandler);
            return newButton;
        }

        public Button ColorPaletteButton(WriteableBitmap bitmap, Color color, MouseButtonEventHandler mouseButtonEventHandler)
        {
            Button newButton = new Button();
            newButton.Width = 40;
            newButton.Height = 40;
            newButton.Margin = new Thickness(2);
            newButton.Padding = new Thickness(2);
            newButton.Content = new Image
            {
                Source = bitmap,
                VerticalAlignment = VerticalAlignment.Center,
                Stretch = Stretch.Uniform,
                Height = 40,
                Width = 40,
                ToolTip = color
            };
            newButton.Name = "_" + color.ToString().Substring(1);
            newButton.PreviewMouseDown += mouseButtonEventHandler;
            return newButton;
        }

        public Button ImagePreviewButton(WriteableBitmap bitmap, int index, int currentBitmapIndex, RoutedEventHandler routedEventHandler)
        {
            Button newButton = new Button();
            Image newImage = new Image
            {
                Source = bitmap,
                VerticalAlignment = VerticalAlignment.Center,
                Stretch = Stretch.Uniform,
                Height = 120,
                Width = 120,
            };
            newButton.Content = newImage;
            newButton.IsEnabled = (currentBitmapIndex == index) ? false : true;
            newButton.Width = 180;
            newButton.Height = 180;
            newButton.Margin = new Thickness(2, 1, 2, 1);
            newButton.Name = "ImagePreview" + index.ToString();
            //newButton.SetResourceReference(System.Windows.Controls.Control.StyleProperty, "AnimationButton");
            newButton.AddHandler(System.Windows.Controls.Button.ClickEvent, routedEventHandler);
            return newButton;
        }
    }
}
