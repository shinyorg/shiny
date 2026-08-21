using System;
using Foundation;
using ObjCRuntime;

namespace ShinyLiveActivities
{
    // SWIFT_CLASS_NAMED("ShinyActivityBridge")
    // @interface ShinyActivityBridge : NSObject
    [BaseType(typeof(NSObject))]
    interface ShinyActivityBridge
    {
        // +(BOOL)isSupported;
        [Static]
        [Export("isSupported")]
        bool IsSupported();

        // +(BOOL)areActivitiesEnabled;
        [Static]
        [Export("areActivitiesEnabled")]
        bool AreActivitiesEnabled();

        // +(NSString * _Nullable)startWithAttributes:(NSString *)attributesJson contentState:(NSString *)contentStateJson staleDate:(NSNumber * _Nullable)staleDate relevanceScore:(NSNumber * _Nullable)relevanceScore requestPushToken:(BOOL)requestPushToken error:(NSError **)error;
        [Static]
        [Export("startWithAttributes:contentState:staleDate:relevanceScore:requestPushToken:error:")]
        [return: NullAllowed]
        string Start(string attributesJson, string contentStateJson, [NullAllowed] NSNumber staleDate, [NullAllowed] NSNumber relevanceScore, bool requestPushToken, [NullAllowed] out NSError error);

        // +(void)updateWithId:(NSString *)id contentState:(NSString *)contentStateJson staleDate:(NSNumber * _Nullable)staleDate relevanceScore:(NSNumber * _Nullable)relevanceScore alertTitle:(NSString * _Nullable)alertTitle alertBody:(NSString * _Nullable)alertBody completion:(void (^)(NSError * _Nullable))completion;
        [Static]
        [Export("updateWithId:contentState:staleDate:relevanceScore:alertTitle:alertBody:completion:")]
        void Update(string id, string contentStateJson, [NullAllowed] NSNumber staleDate, [NullAllowed] NSNumber relevanceScore, [NullAllowed] string alertTitle, [NullAllowed] string alertBody, Action<NSError> completion);

        // +(void)endWithId:(NSString *)id contentState:(NSString * _Nullable)contentStateJson dismissAt:(NSNumber * _Nullable)dismissAt completion:(void (^)(NSError * _Nullable))completion;
        [Static]
        [Export("endWithId:contentState:dismissAt:completion:")]
        void End(string id, [NullAllowed] string contentStateJson, [NullAllowed] NSNumber dismissAt, Action<NSError> completion);

        // +(void)endAllWithCompletion:(void (^)(void))completion;
        [Static]
        [Export("endAllWithCompletion:")]
        void EndAll(Action completion);

        // +(NSArray<NSDictionary<NSString *, NSString *> *> *)activeActivities;
        [Static]
        [Export("activeActivities")]
        NSDictionary<NSString, NSString>[] ActiveActivities();

        // +(NSString * _Nullable)pushToStartToken;
        [Static]
        [Export("pushToStartToken")]
        [return: NullAllowed]
        string PushToStartToken();

        // +(void)startObservingWithStarted:(void (^)(NSString *))started token:(void (^)(NSString *, NSString *))token pushToStart:(void (^)(NSString *))pushToStart state:(void (^)(NSString *, NSString *))state;
        [Static]
        [Export("startObservingWithStarted:token:pushToStart:state:")]
        void StartObserving(Action<NSString> started, Action<NSString, NSString> token, Action<NSString> pushToStart, Action<NSString, NSString> state);
    }
}
