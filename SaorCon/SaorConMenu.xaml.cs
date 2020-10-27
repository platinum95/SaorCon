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
            
            Topmost = true;
            foreach ( var device in m_devices )
            {
                var controlBlock = new DeviceControlBlock( device );
                controlBlock.MouseLeftButtonUp += ChildClicked;
                
                if ( m_devices.Count == 1 )
                    controlBlock.Expand();
                
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
            // Collapse all currently expanded blocks
            foreach ( var block in m_controlBlocks.Where( x => x != sender && x.IsExpanded ) )
                block.Collapse();

            ((DeviceControlBlock)sender).Expand();
        }

        protected override void OnDeactivated( EventArgs e )
        {
            base.OnDeactivated( e );
            this.Close();
        }

        // TODO - change list type if we ever add anything other than control blocks to the stackpanel
        private IList<DeviceControlBlock> m_controlBlocks { get => (IList<DeviceControlBlock>)mainStack.Children; }
        private List<IBoseDevice> m_devices;
    }
}
