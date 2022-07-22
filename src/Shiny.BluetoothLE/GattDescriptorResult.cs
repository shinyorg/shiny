using System;

namespace Shiny.BluetoothLE;


public record GattDescriptorResult(
    IGattDescriptor Descriptor,
    byte[]? Data
);