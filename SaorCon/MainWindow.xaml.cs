using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using System.Windows.Markup;
using Microsoft.Win32;

namespace SaorCon
{
    public partial class MainWindow : Window
    {
        private static string Qc35Query =
            "System.Devices.DevObjectType:=5 AND " +
            "System.Devices.Aep.ProtocolId:=\"{E0CBF06C-CD8B-4647-BB8A-263B43F0F974}\" AND " +
            "(System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean#False) AND " +
            "System.DeviceInterface.Bluetooth.VendorId:=158 AND " +
            "(System.DeviceInterface.Bluetooth.ProductId:=16396 OR System.DeviceInterface.Bluetooth.ProductId:=16416 OR System.DeviceInterface.Bluetooth.ProductId:=16420)";

        public MainWindow()
        {
            InitializeComponent();
            m_deviceWatcher = DeviceInformation.CreateWatcher( Qc35Query, new List<string>() { "System.DeviceInterface.Bluetooth.ProductId" } );
            m_deviceWatcher.Added += OnBluetoothDeviceAdded;
            m_deviceWatcher.Removed += OnBluetoothDeviceRemoved;
            m_deviceWatcher.Start();

            SystemEvents.PowerModeChanged += OnPowerChange;
        }

        ~MainWindow()
        {
            SystemEvents.PowerModeChanged -= OnPowerChange;
        }

        private void OnBluetoothDeviceAdded( DeviceWatcher sender, DeviceInformation device )
        {
            if ( m_devices.Find( x => x.DeviceId == device.Id ) == null )
            {
                Task.Factory.StartNew( async () =>
                    {
                        device.Properties.TryGetValue( "System.DeviceInterface.Bluetooth.ProductId", out var pidObj );

                        var pid = pidObj.ToString();
                        IDevice boseDevice;
                        if ( pid == "16420" )
                            boseDevice = new BoseDeviceNC700( await BluetoothDevice.FromIdAsync( device.Id ) );
                        else
                            boseDevice = new BoseDeviceQC35( await BluetoothDevice.FromIdAsync( device.Id ) );

                        m_devices.Add( boseDevice );

                        if ( m_quickMenu != null )
                        {
                            m_quickMenu.OnDeviceAdded( boseDevice );
                        }
                    } );
            }
        }

        private void OnBluetoothDeviceRemoved( DeviceWatcher sender, DeviceInformationUpdate devInfo )
        {
            m_devices.RemoveAll( x => x.DeviceId == devInfo.Id );
            if ( m_quickMenu != null )
            {
                m_quickMenu.OnDeviceRemoved( devInfo.Id );
            }
        }

        /*
         * Show SaorCon quick-menu device list, if not already visible
         */
        private void NotifyIcon_OnMouseUp( object sender, RoutedEventArgs e )
        {
            if ( m_quickMenu != null )
            {
                m_quickMenu.Focus();
                m_quickMenu.Activate();
                m_quickMenu.Show();
                return;
            }

            m_quickMenu = new SaorConMenu( m_devices );
            m_quickMenu.Closed += OnQuickMenuClosed;
            m_quickMenu.Show();
            m_quickMenu.Focus();
            m_quickMenu.Activate();
        }

        protected override void OnClosing( System.ComponentModel.CancelEventArgs e )
        {
            MyNotifyIcon.Dispose();
            base.OnClosing( e );
        }

        private void OnQuickMenuClosed( object sender, EventArgs e )
        {
            m_quickMenu = null;
        }

        private void OnPowerChange ( object s, PowerModeChangedEventArgs e )
        {
            switch ( e.Mode )
            {
                case PowerModes.Resume:
                    m_deviceWatcher.Start();
                    break;
                case PowerModes.Suspend:
                    m_quickMenu = null;
                    m_deviceWatcher.Stop();
                    m_devices.Clear();
                    break;
            }
        }

        private SaorConMenu             m_quickMenu;
        private List<IDevice>       m_devices = new List<IDevice>();
        private DeviceWatcher           m_deviceWatcher;
    }

    // TODO - move commands out to own files
    public abstract class CommandBase<T> : MarkupExtension, ICommand
        where T : class, ICommand, new()
    {
        private static T s_command { get; } = new T();

        public event EventHandler CanExecuteChanged
        {
            add { }
            remove { }
        }

        public virtual bool CanExecute( object parameter )
        {
            return true;
        }

        public override object ProvideValue( IServiceProvider serviceProvider )
        {
            return s_command;
        }

        public abstract void Execute( object parameter );
    }

    public class ExitCommand : CommandBase<ExitCommand>
    {
        public override void Execute( object parameter )
        {
            System.Windows.Application.Current.Shutdown();
        }
    }

    public class ShowSettingsCommand : CommandBase<ShowSettingsCommand>
    {
        public override void Execute( object parameter )
        {
            new SettingsWindow().Show();
        }
    }


}
