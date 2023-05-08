using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SaorCon
{
    public partial class DeviceControlBlock : UserControl, IObserver<DeviceStatusMessage>
    {
        public string   DeviceId        { get => m_device.DeviceId; }
        public bool     IsExpanded      { get; private set; } = false;
        public string   BatteryLevel    { get => $"{((m_device != null && m_device.Connected && m_device is IBatteryProvider batteryProvider) ? batteryProvider.BatteryLevel : Convert.ToInt16(-1))}%"; }

        public DeviceControlBlock( IDevice device )
        {
            DataContext = this;
            InitializeComponent();
            BatteryLevelText.Text = "";

            device_name.Text = device.DisplayName;
            anc_slider.SelectionStart = 0;

            ConnectedGrid.Visibility = Visibility.Collapsed;
            DisconnectedGrid.Visibility = Visibility.Collapsed;
            m_device = device;
            if ( device.SoftConnect )
            {
                if ( device is IBatteryProvider batteryDevice )
                {
                    BatteryLevelText.Text = $"{batteryDevice.BatteryLevel}%";
                    setBatteryIcon();
                }

                if ( device is IAncProvider ancDevice )
                {
                    anc_slider.Value = ancDevice.AncLevel;
                    anc_slider.SelectionEnd = ancDevice.AncLevel;
                    anc_slider.Maximum = ancDevice.AncRange - 1;
                    setAncLevelIcon( ancDevice.AncLevel );
                }
            }

            m_unsubscriber = m_device.Subscribe( this );

            MouseEventHandler hoverStateHandler = delegate { Background = getBackgroundColour(); };
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

        public void OnNext( DeviceStatusMessage value )
        {
            if ( !this.IsInitialized )
            {
                return;
            }

            this.Dispatcher.Invoke(() =>
            {
                switch ( value )
                {
                    case DeviceStatusMessage.ConnectAck:
                        if (IsExpanded)
                        {
                            DisconnectedGrid.Visibility = Visibility.Collapsed;
                            ConnectedGrid.Visibility = Visibility.Visible;
                        }
                        break;

                    case DeviceStatusMessage.Disconnected:
                        if (IsExpanded)
                        {
                            ConnectedGrid.Visibility = Visibility.Collapsed;
                            DisconnectedGrid.Visibility = Visibility.Visible;
                        }
                        break;

                    case DeviceStatusMessage.BatteryLevel:
                        if ( m_device is IBatteryProvider batteryDevice )
                        {
                            BatteryLevelText.Text = $"{batteryDevice.BatteryLevel}%";
                            setBatteryIcon();
                        }
                        break;

                    case DeviceStatusMessage.AncLevel:
                        if ( m_device is IAncProvider ancDevice )
                        {
                            anc_slider.SelectionEnd = ancDevice.AncLevel;
                            if ( !m_ancSet )
                            {
                                anc_slider.Value = ancDevice.AncLevel;
                            }
                        }
                        break;
                }
            });
        }

        public void OnError ( Exception error )
        {
            throw new NotImplementedException();
        }

        public void OnCompleted ()
        {
            throw new NotImplementedException();
        }

        private void ancSlider_OnValueChanged( object sender, RoutedPropertyChangedEventArgs<double> e )
        {
            if ( m_device == null || !m_device.Connected || !(m_device is IAncProvider ancProvider) || ancProvider.AncLevel == anc_slider.Value )
            {
                Console.WriteLine( $"Ignoring ANC set to {e.NewValue}" );
                return;
            }

            m_ancSet = true;
            var newLevel = Convert.ToInt16( e.NewValue );
            Console.WriteLine( $"Attempting ANC set to {newLevel}" );
            ancProvider.SetAncLevel( newLevel );
            setAncLevelIcon( newLevel );
        }

        
        private void setAncLevelIcon( Int16 level )
        {
            anc_level_icon_hi.Visibility = Visibility.Hidden;
            anc_level_icon_low.Visibility = Visibility.Hidden;
            anc_level_icon_off.Visibility = Visibility.Hidden;

            switch( level )
            {
                case 0: anc_level_icon_off.Visibility = Visibility.Visible; break;
                case 1: anc_level_icon_low.Visibility = Visibility.Visible; break;
                default: anc_level_icon_hi.Visibility = Visibility.Visible; break;
            }
        }

        private void setBatteryIcon()
        {
            if ( !(m_device is IBatteryProvider batteryProvider ) )
                return;

            BatteryLevelHigh.Visibility = Visibility.Hidden;
            BatteryLevelMid.Visibility = Visibility.Hidden;
            BatteryLevelLow.Visibility = Visibility.Hidden;

            var batteryLevel = batteryProvider.BatteryLevel;

            if ( batteryLevel < 30 )
            {
                BatteryLevelLow.Visibility = Visibility.Visible;
            }
            else if ( batteryLevel < 70 )
            {
                BatteryLevelMid.Visibility = Visibility.Visible;
            }
            else
            {
                BatteryLevelHigh.Visibility = Visibility.Visible;
            }
        }

        private void connectButton_OnClick( object sender, RoutedEventArgs e )
        {
            var res = ( (BoseDeviceQC35)m_device ).Connect();
            connectButton.IsEnabled = false;

            Task.Factory.StartNew( () =>
            {
                res.AsyncWaitHandle.WaitOne();
                Dispatcher.Invoke( () => connectButton.IsEnabled = !m_device.Connected );
            } );
        }

        private SolidColorBrush getBackgroundColour()
        {
            if ( m_isAlone || IsExpanded == false )
            {
                return IsMouseOver ? ThemeManager.BackgroundHover : ThemeManager.BackgroundBase;
            }

            return IsMouseOver ? ThemeManager.SelectedHover : ThemeManager.SelectedBase;
        }

        private bool            m_isAlone;
        private IDisposable     m_unsubscriber;
        private IDevice         m_device;
        private bool            m_ancSet = false;
    }
}
