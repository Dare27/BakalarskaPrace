using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BakalarskaPrace
{
    public partial class WindowResize : Window
    {
        public int newWidth;
        public int newHeight;
        public bool maintainAspectRatio = false;
        public bool resizeContent = false;
        private int lastSizeValue;
        public string position;

        public WindowResize()
        {
            InitializeComponent();
            List<Position> list = new List<Position>();
            list.Add(new Position { ID = 1, Name = "top left" });
            list.Add(new Position { ID = 2, Name = "top middle" });
            list.Add(new Position { ID = 3, Name = "top right" });

            list.Add(new Position { ID = 4, Name = "middle left" });
            list.Add(new Position { ID = 5, Name = "middle" });
            list.Add(new Position { ID = 6, Name = "middle right" });

            list.Add(new Position { ID = 7, Name = "bottom left" });
            list.Add(new Position { ID = 8, Name = "bottom middle" });
            list.Add(new Position { ID = 9, Name = "bottom right" });
            PositionCombobox.ItemsSource = list;
            PositionCombobox.SelectedIndex = 4;
        }

        private void Resize_Click(object sender, RoutedEventArgs e)
        {
            bool widthParsed = int.TryParse(widthTextBox.Text, out newWidth);
            bool heightParsed = int.TryParse(heightTextBox.Text, out newHeight);
            if(widthParsed && heightParsed && newWidth > 0 && newHeight > 0 && newWidth < 513 && newHeight < 513)
            {
                maintainAspectRatio = maintainAspectRatioCheckBox.IsChecked.GetValueOrDefault();
                //resizeContent = resizeContentCheckBox.IsChecked.GetValueOrDefault();
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
                bool widthParsed = int.TryParse(widthTextBox.Text, out newWidth);
                if (heightTextBox != null && widthParsed)
                {
                    lastSizeValue = newWidth;
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
                bool heightParsed = int.TryParse(heightTextBox.Text, out newHeight);
                if (widthTextBox != null && heightParsed)
                {
                    lastSizeValue = newHeight;
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

        private void PositionCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PositionCombobox.SelectedItem != null)
            {
                string id = PositionCombobox.SelectedValue.ToString();
                position = ((Position)PositionCombobox.SelectedItem).Name.ToString();
            }

        }

        public class Position
        {
            public int ID { get; set; }
            public string Name { get; set; }
        }
    }
}
