using System;


namespace Shiny.BluetoothLE.Hosting
{
    public class GattDescriptor : IGattDescriptor
    {
        public GattDescriptor()
        {

        }
//        public async Task Init(GattLocalCharacteristic characteristic)
//        {
//            var result = await characteristic.CreateDescriptorAsync(
//                this.Uuid,
//                new GattLocalDescriptorParameters
//                {
//                    ReadProtectionLevel = GattProtectionLevel.Plain,
//                    WriteProtectionLevel = GattProtectionLevel.Plain
//                    //Vale = null
//                }
//            );
//            this.native = result.Descriptor;
//        }

    }
}
