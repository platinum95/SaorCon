using System;

namespace SaorCon
{
    public enum BoseMessage
    {
        ConnectAckMessage,
        DisconnectMessage,
        AncLevelMessage,
        BatteryLevelMessage,
        Unknown
    }

    public interface IBoseDevice : IObservable<BoseMessage>
    {
        bool Connected { get; }
        bool SoftConnect { get; }
        Int16 AncLevel { get; }
        Int16 BatteryLevel { get; }
        string DeviceId { get; }
        string DeviceName { get; }
        Int16 AncRange { get; }

        void SetAncLevel ( Int16 level );

    }
}
