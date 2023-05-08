using System;

namespace SaorCon
{
    public enum DeviceStatusMessage
    {
        ConnectAck,
        Disconnected,
        AncLevel,
        BatteryLevel,
        Unknown
    }

    public interface IDevice : IObservable<DeviceStatusMessage>
    {
        bool Connected { get; }
        bool SoftConnect { get; }
        string DisplayName { get; }
        string DeviceId { get; }
    }

    public interface IBatteryProvider
    {
        Int16 BatteryLevel { get; }
    }

    public interface IAncProvider
    {
        Int16 AncLevel { get; }
        Int16 AncRange { get; }
        void SetAncLevel( Int16 level );
    }

}
