using System;
using System.Threading.Tasks;
using Android.Gms.Extensions;
using Android.Runtime;
using Firebase.Messaging;


namespace Shiny.Push
{
    public class FirebaseManager
    {
        public async Task<string> RequestToken()
        {
            FirebaseMessaging.Instance.AutoInitEnabled = true;

            var task = await FirebaseMessaging.Instance.GetToken();
            var token = task.JavaCast<Java.Lang.String>().ToString();
            return token;
        }


        public async Task UnRegister()
        {
            FirebaseMessaging.Instance.AutoInitEnabled = false;
            await Task.Run(() => FirebaseMessaging.Instance.DeleteToken()).ConfigureAwait(false);
        }
    }
}
