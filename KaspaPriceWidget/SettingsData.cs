using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace KaspaPriceWidget
{
    public class SettingsData
    {
        public double FontSize { get; set; }
        public string FontColor { get; set; }
        public double WindowLeft { get; set; }
        public double WindowTop { get; set; }
        public double Opacity { get; set; }
        public string BackgroundColor { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public bool isLocked { get; set; }

        public SettingsData()
        {
            FontSize = 0.0;
            FontColor = String.Empty;
            WindowLeft = 0.0;
            WindowTop = 0.0;
            Opacity = 0.0;
            BackgroundColor = String.Empty;
            Height = 135;
            Width = 135;
            isLocked = false;
        }
    }
}

