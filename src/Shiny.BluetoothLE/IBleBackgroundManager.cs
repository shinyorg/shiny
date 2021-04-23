//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Shiny.BluetoothLE
//{
//    public enum BleRadioState
//    {
//        On,
//        Off,
//        Unknown
//    }

//    public interface IBleRadioStateDelegate
//    {
//        Task OnStateChanged(BleRadioState status);
//    }

//    public interface IBlePeripheralIdentifier
//    {
//        Task<string> Identify(IPeripheral peripheral);
//    }


//    public class PeripheralNameBleIdentifer : IBlePeripheralIdentifier
//    {
//        // could do static manufacturer data, custom
//        public Task<string> Identify(IPeripheral peripheral)
//            => Task.FromResult(peripheral.Name);
//    }


//    public interface IBlePeripheralDelegate
//    {
//        // don't call cancelconnection, it won't remove registration, could wrap this
//        Task OnStatusChanged(IPeripheral peripheral);
//        Task OnCharacteristicAction(GattCharacteristicResult result);
//    }


//    public struct BleBackgroundRegistration
//    {
//        public string Identifier { get; }
//    }

//    // if a registration comes in that is in the repo, it will be disconnected and the delegate will not fire
//    public interface IBleBackgroundManager
//    {
//        Task<IReadOnlyList<BleBackgroundRegistration>> GetRegistrations();

//        // delegate and identifier will be trystarted via msft extension di
//        Task Register<TDelegate>(IPeripheral peripheral); // defaults to device name
//        Task Register<TDelegate, TIdentifier>(IPeripheral peripheral)
//            where TDelegate : IBlePeripheralDelegate
//            where TIdentifier : IBlePeripheralIdentifier;

//        Task UnRegister(string identifer);
//    }
//}
