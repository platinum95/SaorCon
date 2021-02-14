using System;
using System.Windows;
using System.Windows.Media;

namespace SaorCon
{
    public class ThemeManager : ResourceDictionary
    {
        public enum Themes
        {
            Dark,
            Light
        };

        public static SolidColorBrush BackgroundBase;
        public static SolidColorBrush BackgroundHover;
        public static SolidColorBrush SelectedBase;
        public static SolidColorBrush SelectedHover;
        public static SolidColorBrush TextColour;

        public Uri DarkSource { get; set; }
        public Uri LightSource { get; set; }

        private SolidColorBrush GetThemeColour( string colourName )
        {
            return new SolidColorBrush( (Color)Application.Current.Resources[colourName] );
        }

        public void SetTheme( Themes theme )
        {
            switch ( theme )
            {
                case Themes.Dark:
                    if ( DarkSource == null )
                        return;
                    Source = DarkSource;
                    break;
                case Themes.Light:
                    if ( LightSource == null )
                        return;
                    Source = LightSource;
                    break;
            };
            BackgroundBase = GetThemeColour( "BackgroundBase" );
            BackgroundHover = GetThemeColour( "BackgroundHover" );
            SelectedBase = GetThemeColour( "SelectedBase" );
            SelectedHover = GetThemeColour( "SelectedHover" );
            TextColour = GetThemeColour( "TextColour" );
        }
    }
}
