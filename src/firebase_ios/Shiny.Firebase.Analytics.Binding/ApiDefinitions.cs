using System;
using Foundation;

namespace Shiny.Firebase.Analytics.Binding
{
    [BaseType(typeof(NSObject))]
    interface FirebaseApplication
    {
        [Static]
        [Export("autoConfigure")]
        void AutoConfigure();

        [Static]
        [Export("configure:gcmSenderId:")]
        void Configure(string googleAppId, string gcmSenderId);
    }


    // @interface FirebaseAnalytics : NSObject
    [BaseType (typeof(NSObject))]
	interface FirebaseAnalytics
	{
		// -(void)logEventWithEventName:(NSString * _Nonnull)eventName parameters:(NSDictionary<NSString *,id> * _Nonnull)parameters;
		[Static]
		[Export ("logEventWithEventName:parameters:")]
		void LogEvent (string eventName, NSDictionary<NSString, NSObject> parameters);

        // -(void)getAppInstanceIdWithCompletion:(void (^ _Nonnull)(NSString * _Nullable))completion;
        [Static]
        [Export ("getAppInstanceIdWithCompletion:")]
		[Async]
		void GetAppInstanceId (Action<NSString> completion);

        // -(void)setUserIdWithUserId:(NSString * _Nonnull)userId;
        [Static]
        [Export ("setUserIdWithUserId:")]
		void SetUserId (string userId);

        // -(void)setUserPropertyWithPropertyName:(NSString * _Nonnull)propertyName value:(NSString * _Nonnull)value;
        [Static]
        [Export ("setUserProperty:value:")]
		void SetUserProperty (string propertyName, string value);

        // -(void)setSessionTimeoutWithSeconds:(NSInteger)seconds;
        [Static]
        [Export ("setSessionTimeoutWithSeconds:")]
		void SetSessionTimeout (nint seconds);

        // -(void)resetAnalyticsData;
        [Static]
        [Export ("resetAnalyticsData")]
		void ResetAnalyticsData ();
	}
}
