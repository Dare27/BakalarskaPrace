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
            newWidth = int.Parse(widthTextBox.Text);
            newHeight = int.Parse(heightTextBox.Text);
            maintainAspectRatio = maintainAspectRatioCheckBox.IsChecked.GetValueOrDefault();
            this.Close();
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
                    lastSizeValue = int.Parse(widthTextBox.Text);
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
                    lastSizeValue = int.Parse(heightTextBox.Text);
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
