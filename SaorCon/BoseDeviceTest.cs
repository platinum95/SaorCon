using System;
using System.Collections.Generic;

namespace SaorCon
{
    public class BoseDeviceTest : IBatteryProvider, IAncProvider
    {
        public bool     Connected       { get; set; } = false;
        public bool     SoftConnect     { get; set; } = false;
        public Int16    AncLevel        { get; set; }
        public Int16    BatteryLevel    { get; set; }
        public string   DisplayName     { get; } = "Test Device";
        public string   DeviceId        { get; } = "TestId";
        public Int16    AncRange        { get; } = 16;

        public void SetAncLevel ( Int16 level )
        {}

        public IDisposable Subscribe ( IObserver<DeviceStatusMessage> observer )
        {
            if ( !m_observers.Contains( observer ) )
                m_observers.Add( observer );

            return new BoseUnsubscriber<DeviceStatusMessage>( m_observers, observer );
        }

        private List<IObserver<DeviceStatusMessage>> m_observers = new List<IObserver<DeviceStatusMessage>>();
    }
}
