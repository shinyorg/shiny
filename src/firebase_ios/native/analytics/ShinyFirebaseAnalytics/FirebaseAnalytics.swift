import Foundation
import FirebaseAnalytics

@objc(FirebaseAnalytics)
public class FirebaseAnalytics : NSObject {
    
    @objc
    public static func logEvent(eventName: String, parameters: Dictionary<String, Any>) {
        Analytics.logEvent(eventName, parameters: parameters)
    }
    
    @objc
    public static func getAppInstanceId(completion: @escaping (String?) -> Void) {
        completion(Analytics.appInstanceID())
    }
    
    @objc
    public static func setUserId(userId: String) {
        Analytics.setUserID(userId)
    }
    
    @objc
    public static func setUserProperty(propertyName: String, value: String) {
        Analytics.setUserProperty(value, forName: propertyName)
    }
    
    @objc
    public static func setSessionTimeout(seconds: Int) {
        Analytics.setSessionTimeoutInterval(TimeInterval(seconds))
    }
    
    @objc
    public static func resetAnalyticsData() {
        Analytics.resetAnalyticsData()
    }
}
