namespace Shiny.Printers.Blazor;


/// <summary>Which browser API carries the print byte stream.</summary>
public enum BrowserPrinterTransport
{
    /// <summary>Web Bluetooth (<c>navigator.bluetooth</c>) - GATT, mirrors <c>Shiny.Printers.BluetoothLE</c>.</summary>
    WebBluetooth,

    /// <summary>Web Serial (<c>navigator.serial</c>) - USB-serial / RS-232 receipt printers.</summary>
    WebSerial,

    /// <summary>WebUSB (<c>navigator.usb</c>) - a claimed bulk OUT endpoint on the raw USB device.</summary>
    WebUsb
}


/// <summary>Serial parity bit. Maps to the Web Serial <c>parity</c> option.</summary>
public enum WebSerialParity
{
    None,
    Even,
    Odd
}


/// <summary>Serial flow control. Maps to the Web Serial <c>flowControl</c> option.</summary>
public enum WebSerialFlowControl
{
    None,
    Hardware
}
