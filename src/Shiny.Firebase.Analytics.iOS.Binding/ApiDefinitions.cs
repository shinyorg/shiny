using System;
using Foundation;

namespace Shiny.Firebase.Analytics.iOS.Binding
{
    [BaseType(typeof(NSObject))]
    interface FirebaseApplication
    {
        [Static]
        [Export("autoConfigure")]
        void AutoConfigure();

        [Static]
        [Export("configure:gcmSenderId:apiKey:projectId")]
        bool Configure(string googleAppId, string gcmSenderId, string? apiKey, string? projectId);
        
        [Static]
        [Export("isConfigured")]
        bool IsConfigured { get; }
    }


    [BaseType (typeof(NSObject))]
	interface FirebaseAnalytics
	{
		[Static]
		[Export ("logEventWithEventName:parameters:")]
		void LogEvent (string eventName, NSDictionary<NSString, NSObject> parameters);

        [Static]
        [Export ("getAppInstanceIdWithCompletion:")]
		[Async]
		void GetAppInstanceId (Action<NSString> completion);

        [Static]
        [Export ("setUserIdWithUserId:")]
		void SetUserId (string userId);

        [Static]
        [Export ("setUserProperty:value:")]
		void SetUserProperty (string propertyName, string value);

        [Static]
        [Export ("setSessionTimeoutWithSeconds:")]
		void SetSessionTimeout (nint seconds);

        [Static]
        [Export ("resetAnalyticsData")]
		void ResetAnalyticsData ();
	}
}
