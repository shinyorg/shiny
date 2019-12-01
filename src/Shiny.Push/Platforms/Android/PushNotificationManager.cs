using System;
using System.Threading.Tasks;
using Android.Gms.Common;
using Android.Gms.Tasks;
using Firebase.Iid;
using Firebase.Messaging;

namespace Shiny.Push
{
    public class PushNotificationManager : Java.Lang.Object, IOnCompleteListener, IPushNotificationManager
    {
        TaskCompletionSource<object> taskSrc = null;

        public void OnComplete(Android.Gms.Tasks.Task task)
        {
            if (task.IsSuccessful)
                this.taskSrc?.TrySetResult(task.Result);
            else
                this.taskSrc?.TrySetException(task.Exception);
        }

        //<receiver
        //    android:name="com.google.firebase.iid.FirebaseInstanceIdInternalReceiver"
        //    android:exported="false" />
        //<receiver
        //    android:name="com.google.firebase.iid.FirebaseInstanceIdReceiver"
        //    android:exported="true"
        //    android:permission="com.google.android.c2dm.permission.SEND">
        //    <intent-filter>
        //        <action android:name="com.google.android.c2dm.intent.RECEIVE" />
        //        <action android:name="com.google.android.c2dm.intent.REGISTRATION" />
        //        <category android:name="${applicationId}" />
        //    </intent-filter>
        //</receiver>
        public async Task<PushAccessState> RequestAccess()
        {
            //FirebaseMessaging.Instance.AutoInitEnabled = true;

            this.taskSrc = new TaskCompletionSource<object>();
            FirebaseInstanceId.Instance.GetInstanceId().AddOnCompleteListener(this);
            await this.taskSrc.Task;



            //int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            //if (resultCode != ConnectionResult.Success)
            //{
            //    if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
            //        msgText.Text = GoogleApiAvailability.Instance.GetErrorString(resultCode);
            //    else
            //    {
            //        msgText.Text = "This device is not supported";
            //        Finish();
            //    }
            //    return false;
            //}
            //else
            //{
            //    msgText.Text = "Google Play Services is available.";
            //    return true;
            //}
            throw new NotImplementedException();
        }
    }
}
