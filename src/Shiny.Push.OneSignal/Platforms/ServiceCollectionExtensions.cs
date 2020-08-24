using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Notifications;
using Shiny.Push;
using Shiny.Push.OneSignal;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        public static bool UseOneSignalPush(this IServiceCollection services,
                                            Type delegateType,
                                            OneSignalPushConfig config,
                                            params NotificationCategory[] categories)
        {
#if NETSTANDARD2_0
            return false;
#else
            services.AddSingleton(config);
            services.RegisterModule(new PushModule(
                typeof(Shiny.Push.OneSignal.PushManager),
                delegateType,
                categories
            ));
            return true;
#endif
        }


        public static bool UseOneSignalPush<TPushDelegate>(this IServiceCollection services,
                                                           OneSignalPushConfig config,
                                                           params NotificationCategory[] categories)
            where TPushDelegate : class, IPushDelegate
            => services.UseOneSignalPush(
                typeof(TPushDelegate),
                config,
                categories
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
