using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace BakalarskaPrace
{
    /// <summary>
    /// Interakční logika pro WindowStartup.xaml
    /// </summary>
    public partial class WindowStartup : Window
    {
        public int newWidth;
        public int newHeight;
        private int maxWidth = 1024;
        private int maxHeight = 1024;
        public bool maintainAspectRatio = false;
        public bool resizeContent = false;
        private int lastSizeValue;

        public WindowStartup()
        {
            InitializeComponent();
        }

        public void MainWindow_Activated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            bool widthParsed = int.TryParse(widthTextBox.Text, out newWidth);
            bool heightParsed = int.TryParse(heightTextBox.Text, out newHeight);
            if (newWidth < 1 || newHeight < 1)
            {
                if (newWidth < 1) 
                {
                    WidthWarning.Content = "Width has to be higher than 0";
                    WidthWarning.Visibility = Visibility.Visible;
                }
                if (newHeight < 1)
                {
                    HeightWarning.Content = "Height has to be higher than 0";
                    HeightWarning.Visibility = Visibility.Visible;
                }
            }
            else if (newWidth >= maxWidth || newHeight >= maxHeight)
            {
                if (newWidth >= maxWidth)
                {
                    WidthWarning.Content = "Width has to be lower than 1024";
                    WidthWarning.Visibility = Visibility.Visible;
                }
                if (newHeight >= maxHeight)
                {
                    HeightWarning.Content = "Height has to be lower than 1024";
                    HeightWarning.Visibility = Visibility.Visible;
                }
            }
            else if (widthParsed == false || heightParsed == false) 
            {
                if (widthParsed == false)
                {
                    WidthWarning.Content = "Width has to be number";
                    WidthWarning.Visibility = Visibility.Visible;
                }
                if (heightParsed == false)
                {
                    HeightWarning.Content = "Height has to be number";
                    HeightWarning.Visibility = Visibility.Visible;
                }
            }
            else
            {
                maintainAspectRatio = maintainAspectRatioCheckBox.IsChecked.GetValueOrDefault();
                this.Close();
            }
            
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Width_TextChanged(object sender, RoutedEventArgs e)
        {

            if (maintainAspectRatio)
            {
                heightTextBox.Text = widthTextBox.Text;
            }
            else
            {
                if (heightTextBox != null)
                {
                    int.TryParse(widthTextBox.Text, out lastSizeValue);
                }
            }
        }

        private void Height_TextChanged(object sender, RoutedEventArgs e)
        {
            if (maintainAspectRatio)
            {
                widthTextBox.Text = heightTextBox.Text;
            }
            else
            {
                if (widthTextBox != null)
                {
                    int.TryParse(heightTextBox.Text, out lastSizeValue);
                }
            }
        }

        private void MaintainAspectRatio_Checked(object sender, RoutedEventArgs e)
        {
            maintainAspectRatio = true;
            widthTextBox.Text = lastSizeValue.ToString();
            heightTextBox.Text = lastSizeValue.ToString();
        }

        private void MaintainAspectRatio_Unchecked(object sender, RoutedEventArgs e)
        {
            maintainAspectRatio = false;
        }
    }
}
