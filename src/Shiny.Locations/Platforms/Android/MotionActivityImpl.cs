using System;
using System.Reactive.Linq;
using Android.App;
//using Android.Gms.Awareness;
using Android.Gms.Common.Apis;
using Android.Gms.Extensions;
using Android.Gms.Location;
using Android.Gms.Tasks;


namespace Shiny.Locations
{
    public class MotionActivityImpl : Java.Lang.Object,
                                      IMotionActivity,
                                      IOnSuccessListener,
                                      IOnFailureListener
    {
        public bool IsSupported => true;

        //<uses-permission android:name="com.google.android.gms.permission.ACITIVITY_RECOGNITION" />
        public IObservable<MotionActivityEvent> WhenActivityChanged() => Observable.Create<MotionActivityEvent>(async ob =>
        {
            var client1 = await new GoogleApiClient.Builder(Application.Context)
                //.AddApi(Awareness)
                .AddConnectionCallbacks(() =>
                {
                    //Connected Successful
                })
                .BuildAndConnectAsync((i) => { });

            //SnapshotClient.GetDetectedActivityAsync()
            //var request = new ActivityTransition.Builder();
            //new ActivityTransitionRequest();
            //new ActivityTransition.Builder();
            //new Awareness().api.GetDetectedActivityAsync(null).Result.Status.HasResolution;
            var client = ActivityRecognition.GetClient(Application.Context);

            var task = client.RequestActivityUpdates(0, null);
            task.AddOnSuccessListener(this);
            task.AddOnFailureListener(this);

            return () => { };
        });


        public void OnFailure(Java.Lang.Exception e)
        {
            throw new NotImplementedException();
        }


        public void OnSuccess(Java.Lang.Object result)
        {
            throw new NotImplementedException();
        }
    }
}
