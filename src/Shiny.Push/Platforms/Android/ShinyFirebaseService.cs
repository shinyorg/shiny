using System;
using Android.App;
using Android.Runtime;
using Firebase.Iid;


namespace Shiny.Push.Platforms.Android
{
    [Service]
    [IntentFilter(new[] { "" })]
    public class FirebaseService : FirebaseInstanceIdService
    {
        protected FirebaseService(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }


        public override void OnTokenRefresh()
        {
            var token = FirebaseInstanceId.Instance.Id;
        }
    }
}
