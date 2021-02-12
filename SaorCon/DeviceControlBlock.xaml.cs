using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    public enum BatteryIconStates2
    {
        Full,
        Mid,
        Low
    };
    /// <summary>
    /// Interaction logic for DeviceControlBlock.xaml
    /// </summary>
    public partial class DeviceControlBlock : UserControl, IObserver<BoseMessage>
    {

        public string DeviceId { get => m_device.DeviceId; }
        public bool IsExpanded { get; private set; } = false;
        public string BatteryLevel {
            get
            {
                return $"{( ( m_device != null && m_device.Connected ) ? m_device.BatteryLevel : Convert.ToInt16( -1 ) )}%";
            }
            set
            {
                m_batteryLevel = Convert.ToInt16( value );
                //OnPropertyChanged( nameof( BatteryLevel ) );
            }
        }

        private Int16 m_batteryLevel;
        public enum BatteryIconStates
        {
            Full,
            Mid,
            Low
        };
        public BatteryIconStates BatteryIconState {
            get
            {
                if( m_device != null && m_device.SoftConnect )
                {
                    var batteryLevel = m_device.BatteryLevel;
                    if ( batteryLevel < 30 )
                        return BatteryIconStates.Low;
                    else if ( batteryLevel < 70 )
                        return BatteryIconStates.Mid;
                    else
                        return BatteryIconStates.Full;
                }
                return BatteryIconStates.Low;
            }
        }

        public DeviceControlBlock( IBoseDevice device )
        {
            DataContext = this;
            InitializeComponent();
            BatteryLevelText.Text = "";

            device_name.Text = device.DeviceName;
            anc_slider.SelectionStart = 0;

            ConnectedGrid.Visibility = Visibility.Collapsed;
            DisconnectedGrid.Visibility = Visibility.Collapsed;
            m_device = device;
            if ( device.SoftConnect )
            {
                BatteryLevelText.Text = $"{device.BatteryLevel}%";
                anc_slider.Value = device.AncLevel;
                anc_slider.SelectionEnd = device.AncLevel;

                setAncLevelIcon( device.AncLevel );
                SetBatteryIcon();
            }
            
            m_unsubscriber = m_device.Subscribe( this );

            MouseEventHandler hoverStateHandler = delegate { Background = GetBackgroundColour(); };
            MouseEnter += hoverStateHandler;
            MouseLeave += hoverStateHandler;
        }

        // TODO - this should probably be in some kind of event handler
        public void Cleanup()
        {
            m_unsubscriber.Dispose();
        }

        public void Collapse()
        {
            if ( !IsExpanded )
                return;

            m_isAlone = false;
            IsExpanded = false;
            ConnectedGrid.Visibility = Visibility.Collapsed;
            DisconnectedGrid.Visibility = Visibility.Collapsed;

            Background = ThemeManager.BackgroundBase;
        }

        public void Expand( bool isAlone )
        {
            if ( IsExpanded )
                return;

            m_isAlone = isAlone;
            IsExpanded = true;
            if ( m_device.Connected )
                ConnectedGrid.Visibility = Visibility.Visible;
            else
                DisconnectedGrid.Visibility = Visibility.Visible;

            if ( !m_isAlone )
                this.Background = ThemeManager.SelectedBase;
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

        private void SetBatteryIcon()
        {
            BatteryLevelHigh.Visibility = Visibility.Hidden;
            BatteryLevelMid.Visibility = Visibility.Hidden;
            BatteryLevelLow.Visibility = Visibility.Hidden;

            var batteryLevel = m_device.BatteryLevel;

            if (batteryLevel < 30)
            {
                BatteryLevelLow.Visibility = Visibility.Visible;
            }
            else if (batteryLevel < 70)
            {
                BatteryLevelMid.Visibility = Visibility.Visible;
            }
            else
            {
                BatteryLevelHigh.Visibility = Visibility.Visible;
            }
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
                        if ( IsExpanded )
                        {
                            DisconnectedGrid.Visibility = Visibility.Collapsed;
                            ConnectedGrid.Visibility = Visibility.Visible;
                        }
                        break;
                    case BoseMessage.DisconnectMessage:
                        if ( IsExpanded )
                        {
                            ConnectedGrid.Visibility = Visibility.Collapsed;
                            DisconnectedGrid.Visibility = Visibility.Visible;
                        }
                        break;
                    case BoseMessage.BatteryLevelMessage:
                        BatteryLevelText.Text = $"{m_device.BatteryLevel}%";
                        SetBatteryIcon();
                        break;
                    case BoseMessage.AncLevelMessage:
                        anc_slider.SelectionEnd = m_device.AncLevel;
                        if ( !m_ancSet )
                        {
                            anc_slider.Value = m_device.AncLevel;
                            m_ancSet = true;
                        }
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

        private SolidColorBrush GetBackgroundColour()
        {
            if ( m_isAlone || IsExpanded == false )
                return IsMouseOver ? ThemeManager.BackgroundHover : ThemeManager.BackgroundBase;

            return IsMouseOver ? ThemeManager.SelectedHover : ThemeManager.SelectedBase;
        }

        public ImageSource      BatteryIconSource { get; private set; }
        private bool            m_isAlone;
        private IDisposable     m_unsubscriber;
        private IBoseDevice     m_device;
        private bool            m_ancSet = false;
    }
}
