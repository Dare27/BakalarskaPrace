using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BakalarskaPrace
{
    /// <summary>
    /// Interakční logika pro WindowLoad.xaml
    /// </summary>
    public partial class WindowLoadImage : Window
    {
        public bool importSpritesheet = false;
        public bool importImage = true;
        public int imageWidth;
        public int imageHeight;
        public int offsetWidth;
        public int offsetHeight;

        public WindowLoadImage()
        {
            InitializeComponent();
        }

        private void ImportImage_Checked(object sender, RoutedEventArgs e)
        {
            importImage = true;
            importSpritesheet = false;
            //ImportSpritesheet.IsChecked = false;
        }

        private void ImportImage_Unchecked(object sender, RoutedEventArgs e)
        {
            importImage = false;
            importSpritesheet = true;
            //ImportSpritesheet.IsChecked = true;
        }

        private void ImportSpritesheet_Checked(object sender, RoutedEventArgs e)
        {
            importSpritesheet = true;
            importImage = false;
            //ImportImage.IsChecked = false;
        }

        private void ImportSpritesheet_Unchecked(object sender, RoutedEventArgs e)
        {
            importSpritesheet = false;
            importImage = true;
            //ImportImage.IsChecked = true;
        }

        private void Width_TextChanged(object sender, TextChangedEventArgs e)
        {
            imageWidth = int.Parse(widthTextBox.Text);
        }

        private void Height_TextChanged(object sender, TextChangedEventArgs e)
        {
            imageHeight = int.Parse(heightTextBox.Text);
        }

        private void OffsetWidth_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void OffsetHeight_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            importSpritesheet = ImportSpritesheet.IsChecked.GetValueOrDefault();
            importImage = ImportImage.IsChecked.GetValueOrDefault();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
