using IWshRuntimeLibrary;
using System;
using System.Reflection;
using System.Windows;

namespace SaorCon
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            PopulateAboutTab();
            m_startsOnBoot = System.IO.File.Exists( ShortcutPath );
            StartOnBootSelection.IsChecked = m_startsOnBoot;
        }

        private void PopulateAboutTab()
        {          
            var executingAssembly = Assembly.GetExecutingAssembly();

            var version = executingAssembly.GetName().Version;
            var versionSuffix = ( version.Major == 0 ) ? ( version.Minor == 0 ) ? "-alpha" : "-beta" : "";

            AppName.Text = $"{executingAssembly.GetName().Name} v{version}{versionSuffix}";

            string repoUrl = Properties.Resources.SourceRepo;
            RepoHyperlink.NavigateUri = new Uri( repoUrl, UriKind.Absolute );
            RepoLinkText.Text = repoUrl;
            RepoHyperlink.RequestNavigate += ( sender, e ) =>
            {
                System.Diagnostics.Process.Start( e.Uri.ToString() );
            };

            var copyrightAttribs = Assembly.GetExecutingAssembly().GetCustomAttributes( typeof( AssemblyCopyrightAttribute ), true );
            if ( copyrightAttribs.Length > 0 )
                CopyrightText.Text = ( (AssemblyCopyrightAttribute)copyrightAttribs[0] ).Copyright;
            else
                CopyrightText.Text = PlaceholderCopyrightText;
        }

        private void ModifyShortcut( bool shouldStart )
        {
            if ( shouldStart )
            {
                var shell = new WshShell();
                var shortcut = (IWshShortcut)shell.CreateShortcut( ShortcutPath );
                shortcut.Description = "SaorCon shortcut";
                shortcut.TargetPath = Assembly.GetExecutingAssembly().GetName().CodeBase;
                shortcut.Save();
            }
            else
                System.IO.File.Delete( ShortcutPath );
        }

        private void OnOkButtonClicked( object sender, RoutedEventArgs e )
        {
            if ( StartOnBootSelection.IsChecked.HasValue && ( m_startsOnBoot != StartOnBootSelection.IsChecked.Value ) )
                ModifyShortcut( StartOnBootSelection.IsChecked.Value );
            this.Close();
        }

        private bool m_startsOnBoot;
        private static string ShortcutFilename { get; } = "SaorCon.lnk";
        private static string ShortcutPath { get; } = System.IO.Path.Combine( Environment.GetFolderPath( Environment.SpecialFolder.Startup ), ShortcutFilename );
        private static string PlaceholderCopyrightText { get; } = "© 2020 Peter Mullen";
    }
}
