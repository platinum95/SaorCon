using InTheHand.Net;
using InTheHand.Net.Bluetooth;
using InTheHand.Net.Sockets;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;

namespace SaorCon
{
    public enum BoseCommand
    {
        ConnectCommand,
        QueryStatusCommand,
        QueryBatteryCommand,
        SetAncCommand
    }

    public class BoseUnsubscriber<Message> : IDisposable
        where Message : Enum
    {
        private List<IObserver<Message>> _observers;
        private IObserver<Message> _observer;

        public BoseUnsubscriber( List<IObserver<Message>> observers, IObserver<Message> observer )
        {
            this._observers = observers;
            this._observer = observer;
        }

        public void Dispose()
        {
            if ( !( _observer == null ) ) _observers.Remove( _observer );
        }
    }

    public abstract class BoseBluetoothDevice<T,Command> : IDevice
        where T : BoseBluetoothDevice<T, Command>
        where Command : Enum
    {
        public bool     Connected   { get => bluetoothClient != null && bluetoothClient.Connected; }
        public string   DisplayName { get; private set; } = "Unknown Device";
        public string   DeviceId    { get; } = null;
        public bool     SoftConnect { get; protected set; } = false;

        public BoseBluetoothDevice( BluetoothDevice device )
        {
            bluetoothDevice = device;
            DisplayName = device.Name;
            DeviceId = device.DeviceId;

            if ( bluetoothDevice.ConnectionStatus == BluetoothConnectionStatus.Connected )
                Connect();

            bluetoothDevice.ConnectionStatusChanged += DeviceConnectionStateChanged;
        }

        ~BoseBluetoothDevice()
        {
            Disconnect();
            bluetoothDevice = null;
        }

        protected virtual void Disconnect()
        {
            m_readTaskCancellation.Cancel();
            m_requestTaskToken?.Cancel();

            m_requestMutex.WaitOne();
            m_requestMutex.Dispose();

            if ( bluetoothStream != null )
            {
                bluetoothStream.Dispose();
                bluetoothStream.Close();
                bluetoothStream = null;
            }

            if ( bluetoothClient != null && bluetoothClient.Connected )
                bluetoothClient.Dispose();

            bluetoothClient = null;
            SoftConnect = false;

            foreach ( var observer in m_observers )
            {
                observer.OnNext( DeviceStatusMessage.Disconnected );
            }
        }

        private async Task<IAsyncResult> DelayedRequest( CancellationToken token )
        {
            await Task.Delay( 100 );
            if ( token.IsCancellationRequested )
            {
                return Task.CompletedTask;
            }

            m_requestMutex.WaitOne();
            var result = SendCommand( m_requestBuffer.Item1, m_requestBuffer.Item2, true );
            m_requestMutex.ReleaseMutex();

            return Task.FromResult( result );
        }

        protected IAsyncResult SendCommand( Command command, byte[] payload = null, bool force = false )
        {
            // TODO - these should be sent to a fixed-size buffer to avoid a barrage of requests
            if ( !bluetoothClient.Connected )
                throw new Exception();

            var currentTime = Convert.ToUInt64( new TimeSpan( DateTime.Now.Ticks ).TotalMilliseconds );
            if ( !force && currentTime < m_requestCooldown )
            {
                m_requestCooldown = currentTime + 100;

                m_requestTaskToken.Cancel();
                m_requestTaskToken = new CancellationTokenSource();

                m_requestMutex.WaitOne();
                m_requestBuffer = new Tuple<Command, byte[]>( command, payload );
                m_requestMutex.ReleaseMutex();

                CancellationToken token = m_requestTaskToken.Token;
                return Task.Run( async () => { await DelayedRequest( token ); }, m_requestTaskToken.Token );
            }

            m_requestCooldown = currentTime + 100;

            var baseCode = CommandCodes[command];
            byte payloadLength = ( payload != null ) ? Convert.ToByte( payload.Length ) : (byte)0;

            var finalCommandLength = baseCode.Length + payloadLength + 1;

            byte[] finalCommand = new byte[finalCommandLength];

            Array.Copy( baseCode, finalCommand, baseCode.Length );
            finalCommand[baseCode.Length] = Convert.ToByte( payloadLength );
            if ( payloadLength > 0 )
                Array.Copy( payload, 0, finalCommand, baseCode.Length + 1, payloadLength );

            return bluetoothStream.BeginWrite( finalCommand, 0, finalCommand.Length, OnCommandSent, this );
        }

        private void OnCommandSent( IAsyncResult result )
        {
            try
            {
                bluetoothStream.EndWrite( result );
            }
            catch ( Exception )
            {
                // TODO
            }
        }

        private bool TryGetMessageFromHeader( byte[] header, out DeviceStatusMessage message )
        {
            message = default;
            if ( header.Length < 3 )
            {
                return false;
            }

            foreach ( var messagePair in MessageCodes )
            {
                bool found = true;
                for ( int i = 0; i < 3; ++i )
                {
                    if ( !( found = ( header[i] == messagePair.Value[i] ) ) )
                    {
                        break;
                    }
                }
                if ( found )
                {
                    message = messagePair.Key;
                    return true;
                }
            }

            return false;
        }

        private byte[] GetPayloadFromHeader( byte[] header )
        {
            if ( header.Length < 4 )
            {
                // TODO
                return null;
            }
            var payloadLength = header[3];

            if ( payloadLength == 0 )
                return null;

            var payload = new byte[payloadLength];
            bluetoothStream.Read( payload, 0, payload.Length );
            // TODO - don't hang
            return payload;
        }

        private void ReadIncomingMessages()
        {
            if ( !Connected )
                return;

            while ( bluetoothStream.DataAvailable )
            {
                byte[] header = new byte[4];
                bluetoothStream.Read( header, 0, header.Length );

                // TODO - combine these
                var knownMessage = TryGetMessageFromHeader( header, out var message );
                var payload = GetPayloadFromHeader( header );

                if ( !knownMessage )
                    continue;

                m_messageHandlers[message]( (T)this, payload );

                foreach ( var observer in m_observers )
                    observer.OnNext( message );
            }
        }

        public IAsyncResult Connect()
        {
            if ( bluetoothClient != null && bluetoothClient.Connected )
                return null;

            bluetoothClient = new BluetoothClient();

            var address = new BluetoothAddress( unchecked((long)bluetoothDevice.BluetoothAddress) ); // Bitwise conversion
            var bluetoothEndPoint = new BluetoothEndPoint( address, BluetoothService.SerialPort, BoseRFCommChannel );

            return bluetoothClient.BeginConnect( bluetoothEndPoint, new AsyncCallback( OnConnected ), null );
        }

        protected virtual void OnConnected( IAsyncResult result )
        {
            try
            {
                bluetoothClient.EndConnect( result );
                if ( !Connected )
                    throw new Exception();
            }
            catch ( Exception )
            {
                Console.Write( "Failed to connect to device" );
                bluetoothClient = null;
                return;
            }

            bluetoothStream = bluetoothClient.GetStream();

            m_readTaskCancellation = new CancellationTokenSource();
            var token = m_readTaskCancellation.Token;
            Task.Factory.StartNew( () =>
            {
                while ( Connected && !token.IsCancellationRequested )
                {
                    ReadIncomingMessages();
                    Thread.Sleep( 1000 );
                }
            }, token );
        }

        private void DeviceConnectionStateChanged( BluetoothDevice sender, object args )
        {
            var newState = sender.ConnectionStatus;

            if ( newState == BluetoothConnectionStatus.Connected )
            {
                if ( bluetoothClient == null )
                    Connect();
            }
            else
                Disconnect();
        }

        public IDisposable Subscribe( IObserver<DeviceStatusMessage> observer )
        {
            if ( !m_observers.Contains( observer ) )
                m_observers.Add( observer );

            return new BoseUnsubscriber<DeviceStatusMessage>( m_observers, observer );
        }

        protected abstract Dictionary<Command, byte[]> CommandCodes { get; }
        protected abstract Dictionary<DeviceStatusMessage, byte[]> MessageCodes { get; }

        protected delegate void MessageHandler( T sender, byte[] payload = null );
        protected abstract Dictionary<DeviceStatusMessage, MessageHandler> m_messageHandlers { get; }
        protected List<IObserver<DeviceStatusMessage>> m_observers = new List<IObserver<DeviceStatusMessage>>();

        private BluetoothDevice bluetoothDevice;
        private BluetoothClient bluetoothClient;
        private NetworkStream bluetoothStream;
        private CancellationTokenSource m_readTaskCancellation = new CancellationTokenSource();

        private static readonly int BoseRFCommChannel = 8;
        private ulong m_requestCooldown = 0;
        private Tuple<Command, byte[]> m_requestBuffer;
        private CancellationTokenSource m_requestTaskToken = new CancellationTokenSource();
        private Mutex m_requestMutex = new Mutex();
    }


    public class BoseDeviceQC35 : BoseBluetoothDevice<BoseDeviceQC35, BoseCommand>, IBatteryProvider, IAncProvider
    {
        public Int16 AncLevel { get; private set; } = -1;
        public Int16 BatteryLevel { get; private set; } = -1;
        public Int16 AncRange { get; } = 3;

        public BoseDeviceQC35( BluetoothDevice device ) : base( device )
        {}

        protected override void OnConnected( IAsyncResult result )
        {
            base.OnConnected( result );
            SendCommand( BoseCommand.ConnectCommand, force: true );
            SendCommand( BoseCommand.QueryStatusCommand, force: true );
            SendCommand( BoseCommand.QueryBatteryCommand, force: true );
        }

        public void SetAncLevel( short level )
        {
            if ( !Connected )
                return;

            if ( level == 1 )
                level = 3;
            else if ( level == 2 )
                level = 1;
            else if ( level != 0 )
                throw new ArgumentOutOfRangeException( "Invalid ANC level" );

            if ( m_ancLevelSet == level )
                return;

            m_ancLevelSet = level;
            SendCommand( BoseCommand.SetAncCommand, new byte[] { Convert.ToByte( level ) } );
        }

        private static Int16 ConvertAncLevel( byte[] payload )
        {
            if ( payload.Length != 2 )
            {
                //TODO
                return -1;
            }

            Int16 level = Convert.ToInt16( payload[0] );

            if ( level == 1 ) level = 2;
            else if ( level == 3 ) level = 1;
            else if ( level != 0 ) throw new ArgumentException( $"Received invalid ANC level: {level}" );

            return level;
        }

        private static Int16 ConvertBatteryLevel( byte[] payload )
        {
            if ( payload.Length != 1 )
            {
                //TODO
                return -1;
            }

            return Convert.ToInt16( payload[0] );
        }

        protected override Dictionary<BoseCommand, byte[]> CommandCodes { get; } = new Dictionary<BoseCommand, byte[]>
        {
            { BoseCommand.ConnectCommand,      new byte[] { 0x00, 0x01, 0x01 } },
            { BoseCommand.QueryStatusCommand,  new byte[] { 0x01, 0x01, 0x05 } },
            { BoseCommand.QueryBatteryCommand, new byte[] { 0x02, 0x02, 0x01 } },
            { BoseCommand.SetAncCommand,       new byte[] { 0x01, 0x06, 0x02 } }
        };

        protected override Dictionary<DeviceStatusMessage, byte[]> MessageCodes { get; } = new Dictionary<DeviceStatusMessage, byte[]>
        {
            { DeviceStatusMessage.ConnectAck,    new byte[] { 0x00, 0x01, 0x03 } },
            { DeviceStatusMessage.AncLevel,      new byte[] { 0x01, 0x06, 0x03 } },
            { DeviceStatusMessage.BatteryLevel,  new byte[] { 0x02, 0x02, 0x03 } }
        };

        protected override Dictionary<DeviceStatusMessage, MessageHandler> m_messageHandlers { get; } = new Dictionary<DeviceStatusMessage, MessageHandler>()
        {
            { DeviceStatusMessage.ConnectAck, new MessageHandler( (sender, p) => sender.SoftConnect = true ) },
            { DeviceStatusMessage.AncLevel, new MessageHandler( (sender, p) => sender.AncLevel = ConvertAncLevel(p) ) },
            { DeviceStatusMessage.BatteryLevel, new MessageHandler( (sender, p) => sender.BatteryLevel = ConvertBatteryLevel(p) ) }
        };

        private Int16 m_ancLevelSet = -1;
    }

    public class BoseDeviceNC700 : BoseBluetoothDevice<BoseDeviceNC700, BoseCommand>, IBatteryProvider, IAncProvider
    {
        public Int16 AncLevel { get; private set; } = -1;
        public Int16 BatteryLevel { get; private set; } = -1;
        public Int16 AncRange { get; } = 10;

        public BoseDeviceNC700( BluetoothDevice device ) : base( device )
        { }

        protected override void OnConnected( IAsyncResult result )
        {
            base.OnConnected( result );
            SendCommand( BoseCommand.ConnectCommand, force: true );
            SendCommand( BoseCommand.QueryStatusCommand, force: true );
            SendCommand( BoseCommand.QueryBatteryCommand, force: true );
        }

        public void SetAncLevel( Int16 level )
        {
            if ( !Connected )
                return;

            if ( level > 10 || level < 0 )
                throw new ArgumentOutOfRangeException( "Invalid ANC level" );

            var convertedLevel = (short)( 10 - level );
            if ( m_ancLevelSet == convertedLevel )
                return;

            m_ancLevelSet = convertedLevel;
            AncLevel = level;
            SendCommand( BoseCommand.SetAncCommand, new byte[] { Convert.ToByte( convertedLevel ), Convert.ToByte( true ) } );
            // TODO - set task to trigger after ~1 second to call this function again if level ACK not received
        }

        private static Int16 ConvertAncLevel( byte[] payload )
        {
            if ( payload.Length != 3 )
            {
                //TODO
                return -1;
            }

            Int16 level = Convert.ToInt16( payload[1] );

            level = (short)(10 - level);
            return level;
        }

        private static Int16 ConvertBatteryLevel( byte[] payload )
        {
            if ( payload.Length != 4 )
            {
                //TODO
                return -1;
            }

            // First byte is battery from 00-100
            // Second & third bytes seem to be another battery report, from 0000 - 1023
            // Fourth byte seems to be 0 always? Maybe charging flag?
            // Ignore all but the first byte, the rest aren't useful to us yet
            return Convert.ToInt16( payload[0] );
        }

        protected override Dictionary<BoseCommand, byte[]> CommandCodes { get; } = new Dictionary<BoseCommand, byte[]>
        {
            { BoseCommand.ConnectCommand,      new byte[] { 0x00, 0x01, 0x01 } },
            { BoseCommand.QueryStatusCommand,  new byte[] { 0x01, 0x01, 0x05 } },
            { BoseCommand.QueryBatteryCommand, new byte[] { 0x02, 0x02, 0x01 } },
            { BoseCommand.SetAncCommand,       new byte[] { 0x01, 0x05, 0x02 } }
        };

        protected override Dictionary<DeviceStatusMessage, byte[]> MessageCodes { get; } = new Dictionary<DeviceStatusMessage, byte[]>
        {
            { DeviceStatusMessage.ConnectAck,    new byte[] { 0x00, 0x01, 0x03 } },
            { DeviceStatusMessage.AncLevel,      new byte[] { 0x01, 0x05, 0x03 } },
            { DeviceStatusMessage.BatteryLevel,  new byte[] { 0x02, 0x02, 0x03 } }
        };

        protected override Dictionary<DeviceStatusMessage, MessageHandler> m_messageHandlers { get; } = new Dictionary<DeviceStatusMessage, MessageHandler>()
        {
            { DeviceStatusMessage.ConnectAck, new MessageHandler( (sender, p) => sender.SoftConnect = true ) },
            { DeviceStatusMessage.AncLevel, new MessageHandler( (sender, p) => sender.AncLevel = ConvertAncLevel(p) ) },
            { DeviceStatusMessage.BatteryLevel, new MessageHandler( (sender, p) => sender.BatteryLevel = ConvertBatteryLevel(p) ) }
        };

        private Int16 m_ancLevelSet = -1;
    }
}
