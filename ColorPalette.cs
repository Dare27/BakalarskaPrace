using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BakalarskaPrace
{
    internal class ColorPalette
    {

        private List<Color> colors = new List<Color>();
        private int maxColors = 256;

        public List<Color> Colors
        {
            get { return colors; }
            set { colors = value; }
        }

        public bool AddColor(Color selectedColor) 
        {
            if (colors.Contains(selectedColor) == false && colors.Count < maxColors)
            {
                colors.Add(selectedColor);
                return true;
            }

            return false;
        }

        public void RemoveColor(Color selectedColor)
        {
            colors.Remove(selectedColor);
        }

        public void RemoveAllColors()
        {
            colors.Clear();
        }
    }
}
