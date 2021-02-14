using System.Windows;

namespace SaorCon
{
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
