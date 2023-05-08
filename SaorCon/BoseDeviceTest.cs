using System;
using System.Collections.Generic;

namespace SaorCon
{
    public class BoseDeviceTest : IBoseDevice
    {
        public void SetAncLevel ( Int16 level )
        {

        }

        public IDisposable Subscribe ( IObserver<BoseMessage> observer )
        {
            if ( !m_observers.Contains( observer ) )
                m_observers.Add( observer );

            return new BoseUnsubscriber<BoseMessage>( m_observers, observer );
        }

        public bool Connected { get; set; } = false;
        public bool SoftConnect { get; set; } = false;
        public Int16 AncLevel { get; set; }
        public Int16 BatteryLevel { get; set; }
        public string DeviceName { get; } = "Test Device";
        public string DeviceId { get; } = "TestId";
        public Int16 AncRange { get; } = 16;

        private List<IObserver<BoseMessage>> m_observers = new List<IObserver<BoseMessage>>();
    }
}
