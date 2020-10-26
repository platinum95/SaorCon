using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Configuration;
using System.Net.Sockets;
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
using Windows.ApplicationModel.Background;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using InTheHand.Net;
using System.Windows.Markup;
using System.Threading;
using Hardcodet.Wpf.TaskbarNotification;

namespace SaorCon
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static string Qc35Query =
            "System.Devices.DevObjectType:=5 AND " +
            "System.Devices.Aep.ProtocolId:=\"{E0CBF06C-CD8B-4647-BB8A-263B43F0F974}\" AND " +
            "(System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True OR System.Devices.Aep.Bluetooth.IssueInquiry:=System.StructuredQueryType.Boolean#False) AND " +
            "System.DeviceInterface.Bluetooth.VendorId:=158 AND " +
            "System.DeviceInterface.Bluetooth.ProductId:=16396";

        public MainWindow()
        {
            InitializeComponent();
            
            m_deviceWatcher = DeviceInformation.CreateWatcher( Qc35Query );
            m_deviceWatcher.Added += OnBluetoothDeviceAdded;
            m_deviceWatcher.Removed += OnBluetoothDeviceRemoved;
            m_deviceWatcher.Start();
            m_devices.Add( new TestBoseDevice() );
        }

        private void OnBluetoothDeviceAdded( DeviceWatcher sender, DeviceInformation device )
        {
            if ( m_devices.Find( x => x.DeviceId == device.Id ) == null )
                Task.Factory.StartNew( async () =>
                    {
                        var boseDevice = new BoseDevice( await BluetoothDevice.FromIdAsync( device.Id ) );
                        m_devices.Add( boseDevice );
                        if ( m_quickMenu != null )
                            m_quickMenu.OnDeviceAdded( boseDevice );
                    } );
        }

        private void OnBluetoothDeviceRemoved( DeviceWatcher sender, DeviceInformationUpdate devInfo )
        {
            m_devices.RemoveAll( x => x.DeviceId == devInfo.Id );
            if ( m_quickMenu != null )
                m_quickMenu.OnDeviceRemoved( devInfo.Id );
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
                return;
            }

            m_quickMenu = new SaorConMenu( m_devices );
            m_quickMenu.Closed += onQuickMenuClosed;
            m_quickMenu.Show();
            m_quickMenu.Focus();
            m_quickMenu.Activate();
        }

        protected override void OnClosing( System.ComponentModel.CancelEventArgs e )
        {
            MyNotifyIcon.Dispose();
            base.OnClosing( e );
        }

        public static readonly RoutedUICommand ExitSaorCon = new RoutedUICommand( "Exit", "ExitSaorCon", typeof( MainWindow ) );

        private void ExitSaorCon_CanExecute( object sender, CanExecuteRoutedEventArgs e )
        {
            e.CanExecute = true;
        }

        private void ExitSaorCon_Executed( object sender, ExecutedRoutedEventArgs e )
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void onQuickMenuClosed( object sender, EventArgs e )
        {
            m_quickMenu = null;
        }

        private SaorConMenu             m_quickMenu;
        private List<IBoseDevice>       m_devices = new List<IBoseDevice>();
        private DeviceWatcher           m_deviceWatcher;
    }    
}
