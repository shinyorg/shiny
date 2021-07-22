//using System;
//using System.Reactive.Linq;
//using System.Threading.Tasks;
//using HealthKit;

//namespace Shiny.Sensors
//{
//    public class HeartRateMonitorImpl : IHeartRateMonitor
//    {
//        readonly HKHealthStore healthStore = new HKHealthStore();
//        readonly HKUnit unitType = HKUnit.Count.UnitDividedBy(HKUnit.Minute);


//        public bool IsAvailable => throw new NotImplementedException();

//        public async Task<AccessState> RequestAccess()
//        {
//            //var hrId = HKQuantityTypeIdentifier.HeartRate;

//            //var result = await this.healthStore.RequestAuthorizationToShareAsync(null, null);

//            //HKObjectType.
//            //new HKHealthStore().RequestAuthorizationToShareAsync(
//            //    )
//            //HKHealthStore.Notifications.ObserveUserPreferencesDidChange
//            //new HKHealthStore().EnableBackgroundDeliveryAsync
//            //new HKHealthStore().DisableBackgroundDeliveryAsync();
//            return AccessState.NotSupported;
//        }




//        public IObservable<ushort> WhenReadingTaken()
//        {
//            //this.healthStore.ExecuteQuery()
//            return Observable.Empty<ushort>();
//        }
//    }
//}