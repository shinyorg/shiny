using System;
using System.Reactive.Linq;
using Windows.Devices.Enumeration;
using Windows.Foundation;

namespace Shiny.BluetoothLE;


public partial class Peripheral : ICanPairPeripherals
{
    public PairingState PairingStatus => this.DeviceInfo.Pairing.IsPaired ? PairingState.Paired : PairingState.NotPaired;

    public IObservable<bool> PairingRequest(string? pin = null) => Observable.FromAsync(async ct =>
    {
        if (pin.IsEmpty())
        {
            var result = await this.DeviceInfo.Pairing.PairAsync(DevicePairingProtectionLevel.None);
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
            this.DeviceInfo.Pairing.Custom.PairingRequested += handler;
            var result = await this.DeviceInfo.Pairing.Custom.PairAsync(pairingKind);
            return result.Status == DevicePairingResultStatus.Paired;
        }
        finally
        {
            this.DeviceInfo.Pairing.Custom.PairingRequested -= handler;
        }
    });
}
