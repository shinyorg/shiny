//using System.Reactive.Linq;
//using System.Reactive.Subjects;
//using System.Reactive.Threading.Tasks;
//using System.Threading.Tasks;
//using Android.Bluetooth;
//using Android.Bluetooth.LE;
//using Android.Runtime;
//using BluetoothPhy = Android.Bluetooth.BluetoothPhy;

//namespace Shiny.BluetoothLE;


////[Flags] - this is actually a flag
//public enum PhySetting
//{
//    Le1m = 1, // PHY_LE_1M or PHY_LE_1M_MASK = 1
//    Le2m = 2, // PHY_LE_2M or PHY_LE_2M_MASK = 2
//    LeCoded = 3, // PHY_LE_CODED = 3
//    L3CodedMask = 4 // PHY_LE_CODED_MASK = 4
//}

//public partial class Peripheral
//{
//    readonly Subject<(ScanSettingsPhy Tx, ScanSettingsPhy Rx, GattStatus Status, bool IsRead)> phySubj = new();

//    public override void OnPhyRead(BluetoothGatt? gatt, [GeneratedEnum] ScanSettingsPhy txPhy, [GeneratedEnum] ScanSettingsPhy rxPhy, [GeneratedEnum] GattStatus status)
//        => this.phySubj.OnNext((txPhy, rxPhy, status, true));

//    public override void OnPhyUpdate(BluetoothGatt? gatt, [GeneratedEnum] ScanSettingsPhy txPhy, [GeneratedEnum] ScanSettingsPhy rxPhy, [GeneratedEnum] GattStatus status)
//        => this.phySubj.OnNext((txPhy, rxPhy, status, false));


//    public async Task<(ScanSettingsPhy Scan, ScanSettingsPhy Rx)> GetPhy()
//    {
       
//        this.AssertConnection();

//        var task = this.phySubj
//            .Where(x => x.IsRead)
//            .Take(1)
//            .ToTask();

//        this.Gatt!.ReadPhy();
//        var result = await task.ConfigureAwait(false);

//        return default;
//    }


//    public async Task SetPhy(BluetoothPhy tx, BluetoothPhy rx, BluetoothPhyOption option)
//    {
//        this.AssertConnection();

//        var task = this.phySubj
//            .Where(x => !x.IsRead)
//            .Take(1)
//            .ToTask();
        
//        this.Gatt!.SetPreferredPhy(tx, rx, option);
//        await task.ConfigureAwait(false);
//    }
//}