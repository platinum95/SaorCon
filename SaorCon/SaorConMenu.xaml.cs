using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SaorCon
{
    /// <summary>
    /// Interaction logic for SaorConMenu.xaml
    /// </summary>
    public partial class SaorConMenu : Window
    {
        public SaorConMenu( List<IBoseDevice> devices )
        {
            InitializeComponent();
            m_devices = devices;

            Background = ThemeManager.BackgroundBase;
            Topmost = true;
            foreach ( var device in m_devices )
            {
                var controlBlock = new DeviceControlBlock( device );
                controlBlock.MouseLeftButtonUp += ChildClicked;
                
                if ( m_devices.Count == 1 )
                    controlBlock.Expand( true );

                mainStack.Children.Add( controlBlock );
            }

            
        }

        public void OnDeviceAdded( IBoseDevice device )
        {
            var controlBlock = new DeviceControlBlock( device );
            controlBlock.MouseLeftButtonUp += ChildClicked;
            this.Dispatcher.Invoke( () => mainStack.Children.Add( controlBlock ) );
        }

        public void OnDeviceRemoved( string deviceId )
        {
            foreach( var child in mainStack.Children )
            {
                var controlBlock = child as DeviceControlBlock;
                if ( controlBlock.DeviceId != deviceId )
                    continue;

                this.Dispatcher.Invoke( () => mainStack.Children.Remove( controlBlock ) );
                break;
            }
        }

        protected override void OnRenderSizeChanged( SizeChangedInfo sizeInfo )
        {
            base.OnRenderSizeChanged( sizeInfo );
            var workingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = workingArea.Right - this.ActualWidth;
            this.Top = workingArea.Bottom - this.ActualHeight;
        }

        private void ChildClicked( object sender, EventArgs e )
        {
            if ( !( sender is DeviceControlBlock ) )
                return;
            // Collapse all currently expanded blocks
            foreach ( var child in mainStack.Children )
                if ( child is DeviceControlBlock block && block != sender && block.IsExpanded )
                    block.Collapse();

            ((DeviceControlBlock)sender).Expand( mainStack.Children.Count == 1 );
        }

        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            this.Close();
            
            foreach (var child in mainStack.Children )
            {
                if ( child is DeviceControlBlock block )
                {
                    block.Cleanup();
                }
            }
            mainStack.Children.Clear();
        }

        private List<IBoseDevice> m_devices;
    }
}
