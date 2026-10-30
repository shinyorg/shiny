using Foundation;
using ObjCRuntime;

namespace Shiny.Bluetooth.MacOS;


// IOBluetooth's delegates are informal protocols - it checks respondsToSelector: and calls whatever is
// there - so a plain NSObject subclass exporting the selectors is all a delegate needs to be.


[Register("ShinyBluetoothInquiryDelegate")]
internal sealed class InquiryDelegate(Action<IntPtr> onDeviceFound, Action<int, bool> onComplete) : NSObject
{
    [Export("deviceInquiryDeviceFound:device:")]
    public void DeviceFound(IntPtr sender, IntPtr device) => onDeviceFound(device);

    [Export("deviceInquiryComplete:error:aborted:")]
    public void Complete(IntPtr sender, int error, bool aborted) => onComplete(error, aborted);
}


[Register("ShinyBluetoothSdpQueryDelegate")]
internal sealed class SdpQueryDelegate(Action<int> onComplete) : NSObject
{
    [Export("sdpQueryComplete:status:")]
    public void Complete(IntPtr device, int status) => onComplete(status);
}


[Register("ShinyBluetoothPairingDelegate")]
internal sealed class PairingDelegate(Action<int> onFinished) : NSObject
{
    [Export("devicePairingFinished:error:")]
    public void Finished(IntPtr sender, int error) => onFinished(error);
}


[Register("ShinyBluetoothRfcommDelegate")]
internal sealed class RfcommChannelDelegate(MacRfcommChannel owner) : NSObject
{
    [Export("rfcommChannelOpenComplete:status:")]
    public void OpenComplete(IntPtr channel, int status) => owner.OnOpenComplete(status);

    [Export("rfcommChannelData:data:length:")]
    public unsafe void Data(IntPtr channel, IntPtr data, nuint length)
        => owner.OnData(new ReadOnlySpan<byte>((void*)data, checked((int)length)));

    [Export("rfcommChannelWriteComplete:refcon:status:")]
    public void WriteComplete(IntPtr channel, IntPtr refcon, int status) => owner.OnWriteComplete(refcon, status);

    [Export("rfcommChannelClosed:")]
    public void Closed(IntPtr channel) => owner.OnClosed();
}


[Register("ShinyBluetoothChannelOpenObserver")]
internal sealed class ChannelOpenObserver(Action<IntPtr> onOpen) : NSObject
{
    public const string SelectorName = "newChannel:channel:";

    [Export(SelectorName)]
    public void NewChannel(IntPtr notification, IntPtr channel) => onOpen(channel);
}
