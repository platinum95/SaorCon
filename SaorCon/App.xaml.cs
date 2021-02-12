using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace SaorCon
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            foreach ( ResourceDictionary dict in Resources.MergedDictionaries )
                if ( dict is ThemeManager skinDict )
                    skinDict.SetTheme( ThemeManager.Themes.Dark );
        }
    }
}
