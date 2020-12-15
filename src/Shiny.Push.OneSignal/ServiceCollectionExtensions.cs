using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Push;
using Shiny.Push.OneSignal;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static void UseOneSignalPush(this IServiceCollection services,
                                            Type delegateType,
                                            OneSignalPushConfig config)
        {
            services.AddSingleton(typeof(IPushManager), typeof(Shiny.Push.OneSignal.PushManager));
            services.AddSingleton(typeof(IPushDelegate), delegateType);
        }


        public static void UseOneSignalPush<TPushDelegate>(this IServiceCollection services, OneSignalPushConfig config)
            where TPushDelegate : class, IPushDelegate
            => services.UseOneSignalPush(
                typeof(TPushDelegate),
                config
            );
    }
}

//https://documentation.onesignal.com/docs/xamarin-sdk-setup

//<application....>
//  <receiver android:name="com.onesignal.GcmBroadcastReceiver"
//            android:permission="com.google.android.c2dm.permission.SEND" >
//    <intent-filter>
//      <action android:name="com.google.android.c2dm.intent.RECEIVE" />
//      <category android:name="${manifestApplicationId}" />
//    </intent-filter>
//  </receiver>
//</application>
