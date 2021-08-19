using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shiny.Push;
using Shiny.Push.OneSignal;


namespace Shiny
{
    public static class ServiceCollectionExtensions
    {

        public static bool UseOneSignalPush(this IServiceCollection services,
                                            Type delegateType,
                                            string oneSignalAppId)
            => services.UseOneSignalPush(delegateType, new OneSignalPushConfig(oneSignalAppId));


        public static bool UseOneSignalPush(this IServiceCollection services,
                                            Type delegateType,
                                            OneSignalPushConfig config)
        {
#if __ANDROID__ || XAMARIN_IOS
            services.AddSingleton(config);
            services.TryAddSingleton(typeof(IPushManager), typeof(Shiny.Push.OneSignal.PushManager));
            services.AddSingleton(typeof(IPushDelegate), delegateType);
            return true;
#else
            return false;
#endif
        }


        public static bool UseOneSignalPush<TPushDelegate>(this IServiceCollection services, string oneSignalAppId)
            where TPushDelegate : class, IPushDelegate
            => services.UseOneSignalPush(
                typeof(TPushDelegate),
                oneSignalAppId
            );


        public static bool UseOneSignalPush<TPushDelegate>(this IServiceCollection services, OneSignalPushConfig config)
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
