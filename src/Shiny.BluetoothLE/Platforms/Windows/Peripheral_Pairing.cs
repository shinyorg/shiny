using System;
using System.Reactive.Linq;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Shiny.BluetoothLE.Infrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral : ICanPairPeripherals
{
    public PairingState PairingStatus => this.DeviceInfo?.Pairing.IsPaired == true ? PairingState.Paired : PairingState.NotPaired;

    public IObservable<bool> PairingRequest(string? pin = null) => ObservableTask.FromAsync(async ct =>
    {
        var info = this.DeviceInfo ?? throw new BleException("Device is disposed");
        if (pin.IsEmpty())
        {
            var result = await info.Pairing.PairAsync(DevicePairingProtectionLevel.None);
            return result.Status == DevicePairingResultStatus.Paired;
        }

        var handler = new TypedEventHandler<DeviceInformationCustomPairing, DevicePairingRequestedEventArgs>((sender, args) =>
        {
            switch (args.PairingKind)
            {
                case DevicePairingKinds.ConfirmOnly:
                    args.Accept();
                    break;

                case DevicePairingKinds.ProvidePin:
                    using (var def = args.GetDeferral())
                    {
                        args.Accept(pin);
                        def.Complete();
                    }
                    break;
            }
        });

        var pairingKind = pin.IsEmpty()
            ? DevicePairingKinds.ConfirmOnly
            : DevicePairingKinds.ProvidePin;

        try
        {
            info.Pairing.Custom.PairingRequested += handler;
            var result = await info.Pairing.Custom.PairAsync(pairingKind);
            return result.Status == DevicePairingResultStatus.Paired;
        }
        finally
        {
            info.Pairing.Custom.PairingRequested -= handler;
        }
    });
}
