using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE.Central
{
    public static class Dictionaries
    {
        public static string GetCharacteristicDescription(Guid uuid)
            => KnownCharacteristics.ContainsKey(uuid) ? KnownCharacteristics[uuid] : String.Empty;


        public static string GetServiceDescription(Guid uuid)
            => KnownServices.ContainsKey(uuid) ? KnownServices[uuid] : String.Empty;


        public static string GetDescriptorDescription(Guid uuid)
            => KnownDescriptors.ContainsKey(uuid) ? KnownDescriptors[uuid] : String.Empty;


        public static IDictionary<Guid, string> KnownServices = new Dictionary<Guid, string>
        {
            { new Guid("00001811-0000-1000-8000-00805f9b34fb"), "Alert Notification Service" },
            { new Guid("0000180f-0000-1000-8000-00805f9b34fb"), "Battery Service" },
            { new Guid("00001810-0000-1000-8000-00805f9b34fb"), "Blood Pressure" },
            { new Guid("00001805-0000-1000-8000-00805f9b34fb"), "Current Time Service" },
            { new Guid("00001818-0000-1000-8000-00805f9b34fb"), "Cycling Power" },
            { new Guid("00001816-0000-1000-8000-00805f9b34fb"), "Cycling Speed and Cadence" },
            { new Guid("0000180a-0000-1000-8000-00805f9b34fb"), "Peripheral Information" },
            { new Guid("00001800-0000-1000-8000-00805f9b34fb"), "Generic Access" },
            { new Guid("00001801-0000-1000-8000-00805f9b34fb"), "Generic Attribute" },
            { new Guid("00001808-0000-1000-8000-00805f9b34fb"), "Glucose" },
            { new Guid("00001809-0000-1000-8000-00805f9b34fb"), "Health Thermometer" },
            { new Guid("0000180d-0000-1000-8000-00805f9b34fb"), "Heart Rate" },
            { new Guid("00001812-0000-1000-8000-00805f9b34fb"), "Human Interface Peripheral" },
            { new Guid("00001802-0000-1000-8000-00805f9b34fb"), "Immediate Alert" },
            { new Guid("00001803-0000-1000-8000-00805f9b34fb"), "Link Loss" },
            { new Guid("00001819-0000-1000-8000-00805f9b34fb"), "Location and Navigation" },
            { new Guid("00001807-0000-1000-8000-00805f9b34fb"), "Next DST Change Service" },
            { new Guid("0000180e-0000-1000-8000-00805f9b34fb"), "Phone Alert Status Service" },
            { new Guid("00001806-0000-1000-8000-00805f9b34fb"), "Reference Time Update Service" },
            { new Guid("00001814-0000-1000-8000-00805f9b34fb"), "Running Speed and Cadence" },
            { new Guid("00001813-0000-1000-8000-00805f9b34fb"), "Scan Parameters" },
            { new Guid("00001804-0000-1000-8000-00805f9b34fb"), "TX Power" },
            { new Guid("0000ffe0-0000-1000-8000-00805f9b34fb"), "TI SensorTag Smart Keys" },
            { new Guid("f000aa00-0451-4000-b000-000000000000"), "TI SensorTag Infrared Thermometer" },
            { new Guid("f000aa10-0451-4000-b000-000000000000"), "TI SensorTag Accelerometer" },
            { new Guid("f000aa20-0451-4000-b000-000000000000"), "TI SensorTag Humidity" },
            { new Guid("f000aa30-0451-4000-b000-000000000000"), "TI SensorTag Magnometer" },
            { new Guid("f000aa40-0451-4000-b000-000000000000"), "TI SensorTag Barometer" },
            { new Guid("f000aa50-0451-4000-b000-000000000000"), "TI SensorTag Gyroscope" },
            { new Guid("f000aa60-0451-4000-b000-000000000000"), "TI SensorTag Test" },
            { new Guid("f000ccc0-0451-4000-b000-000000000000"), "TI SensorTag Connection Control" },
            { new Guid("f000ffc0-0451-4000-b000-000000000000"), "TI SensorTag OvertheAir Download" },
            { new Guid("713d0000-503e-4c75-ba94-3148f18d941e"), "TXRX_SERV_UUID RedBearLabs Biscuit Service" }
        };


        public static IDictionary<Guid, string> KnownCharacteristics = new Dictionary<Guid, string>
        {
            { new Guid("00002900-0000-1000-8000-00805f9b34fb"), "Characteristic Extended Properties" },
            { new Guid("00002901-0000-1000-8000-00805f9b34fb"), "Characteristic User Description" },
            { new Guid("00002902-0000-1000-8000-00805f9b34fb"), "Client Characteristic Configuration" },
            { new Guid("00002903-0000-1000-8000-00805f9b34fb"), "Server Characteristic Configuration" },
            { new Guid("00002904-0000-1000-8000-00805f9b34fb"), "Characteristic Presentation Format" },
            { new Guid("00002905-0000-1000-8000-00805f9b34fb"), "Characteristic Aggregate Format" },
            { new Guid("00002906-0000-1000-8000-00805f9b34fb"), "Valid Range" },
            { new Guid("00002907-0000-1000-8000-00805f9b34fb"), "External Report Reference" },
            { new Guid("00002908-0000-1000-8000-00805f9b34fb"), "Export Reference" }
        };


        public static IDictionary<Guid, string> KnownDescriptors = new Dictionary<Guid, string>
        {
            { new Guid("00002900-0000-1000-8000-00805f9b34fb"), "Characteristic Extended Properties" },
            { new Guid("00002901-0000-1000-8000-00805f9b34fb"), "Characteristic User Description" },
            { new Guid("00002902-0000-1000-8000-00805f9b34fb"), "Client Characteristic Configuration" },
            { new Guid("00002903-0000-1000-8000-00805f9b34fb"), "Server Characteristic Configuration" },
            { new Guid("00002904-0000-1000-8000-00805f9b34fb"), "Characteristic Presentation Format" },
            { new Guid("00002905-0000-1000-8000-00805f9b34fb"), "Characteristic Aggregate Format" },
            { new Guid("00002906-0000-1000-8000-00805f9b34fb"), "Valid Range" },
            { new Guid("00002907-0000-1000-8000-00805f9b34fb"), "External Report Reference" },
            { new Guid("00002908-0000-1000-8000-00805f9b34fb"), "Export Reference" }
        };
    }
}
