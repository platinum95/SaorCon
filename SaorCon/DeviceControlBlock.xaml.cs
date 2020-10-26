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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SaorCon
{
    /// <summary>
    /// Interaction logic for DeviceControlBlock.xaml
    /// </summary>
    public partial class DeviceControlBlock : UserControl, IObserver<BoseMessage>
    {

        public string DeviceId { get => m_device.DeviceId; }
        public DeviceControlBlock( IBoseDevice device )
        {
            InitializeComponent();

            device_name.Text = device.DeviceName;
            anc_slider.SelectionStart = 0;

            ConnectedGrid.Visibility = Visibility.Collapsed;
            DisconnectedGrid.Visibility = Visibility.Collapsed;
            if ( device.SoftConnect )
            {
                batteryLevel.Text = $"Battery Level: {device.BatteryLevel}%";
                anc_slider.Value = device.AncLevel;
                anc_slider.SelectionEnd = device.AncLevel;

                setAncLevelIcon( device.AncLevel );
            }
            m_device = device;
            m_unsubscriber = m_device.Subscribe( this );
        }

        ~DeviceControlBlock()
        {
            m_unsubscriber.Dispose();
        }

        public void Collapse()
        {
            if ( !m_expanded )
                return;

            m_expanded = false;
            ConnectedGrid.Visibility = Visibility.Collapsed;
            DisconnectedGrid.Visibility = Visibility.Collapsed;

            this.Background = DarkBase;
        }

        public void Expand()
        {
            if ( m_expanded )
                return;

            m_expanded = true;
            if ( m_device.Connected )
                ConnectedGrid.Visibility = Visibility.Visible;
            else
                DisconnectedGrid.Visibility = Visibility.Visible;

            this.Background = SelectedBase;
        }

        protected override void OnMouseEnter( MouseEventArgs e )
        {
            base.OnMouseEnter( e );
            if ( m_expanded )
                this.Background = SelectedHover;
            else
                this.Background = DarkHover;
        }

        protected override void OnMouseLeave( MouseEventArgs e )
        {
            base.OnMouseEnter( e );

            if ( IsMouseOver )
                return;
            if ( m_expanded )
                this.Background = SelectedBase;
            else
                this.Background = DarkBase;
        }


        private void anc_slider_ValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
        {
            if ( m_device == null || !m_device.Connected )
            {
                Console.WriteLine( $"Ignoring ANC set to {e.NewValue}" );
                return;
            }

            var newLevel = Convert.ToInt16( e.NewValue );
            Console.WriteLine( $"Attempting ANC set to {newLevel}" );
            m_device.SetAncLevel( newLevel );
            setAncLevelIcon( newLevel );
        }

        
        private void setAncLevelIcon( Int16 level )
        {
            anc_level_icon_hi.Visibility = Visibility.Hidden;
            anc_level_icon_low.Visibility = Visibility.Hidden;
            anc_level_icon_off.Visibility = Visibility.Hidden;

            if ( level == 0 )
                anc_level_icon_off.Visibility = Visibility.Visible;
            else if ( level == 1 )
                anc_level_icon_low.Visibility = Visibility.Visible;
            else
                anc_level_icon_hi.Visibility = Visibility.Visible;
        }
        
        
        public void OnNext( BoseMessage value )
        {
            if ( !this.IsInitialized )
                return;
            this.Dispatcher.Invoke( () =>
            {
                switch ( value )
                {
                    case BoseMessage.ConnectAckMessage:
                        if ( m_expanded )
                        {
                            DisconnectedGrid.Visibility = Visibility.Collapsed;
                            ConnectedGrid.Visibility = Visibility.Visible;
                        }
                        break;
                    case BoseMessage.DisconnectMessage:
                        if ( m_expanded )
                        {
                            ConnectedGrid.Visibility = Visibility.Collapsed;
                            DisconnectedGrid.Visibility = Visibility.Visible;
                        }
                        break;
                    case BoseMessage.BatteryLevelMessage:
                        batteryLevel.Text = $"Battery Level: {m_device.BatteryLevel}%";
                        break;
                    case BoseMessage.AncLevelMessage:
                        anc_slider.SelectionEnd = m_device.AncLevel;
                        break;
                }
            } );
        }

        public void OnError( Exception error )
        {
            throw new NotImplementedException();
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        private void OnConnectButtonClick( object sender, RoutedEventArgs e )
        {
            var res = ( (BoseDevice)m_device ).Connect();
            connectButton.IsEnabled = false;

            Task.Factory.StartNew( () =>
            {
                res.AsyncWaitHandle.WaitOne();
                Dispatcher.Invoke( () => connectButton.IsEnabled = !m_device.Connected );
            } );
        }

        private IDisposable m_unsubscriber;
        private IBoseDevice m_device;
        private bool m_expanded = false;

        // TODO - implement actual themes
        private SolidColorBrush DarkBase = new SolidColorBrush( Color.FromArgb( 0xFF, 0x2A, 0x2A, 0x2A ) );
        private SolidColorBrush DarkHover = new SolidColorBrush( Color.FromArgb( 0xFF, 0x3A, 0x3A, 0x3A ) );
        private SolidColorBrush SelectedBase = new SolidColorBrush( Color.FromArgb( 0xFF, 0x51, 0x51, 0x51 ) );
        private SolidColorBrush SelectedHover = new SolidColorBrush( Color.FromArgb( 0xFF, 0x5A, 0x5A, 0x5A ) );
    }
}
