import Foundation
import Combine
import FirebaseMessaging
import FirebaseInstallations
import UIKit

@objc(FirebaseMessaging)
public class FirebaseMessaging : NSObject {
    
    @objc(setIsAutoInitEnabled:)
    public static func setIsAutoInitEnabled(enabled: Bool) {
        Messaging.messaging().isAutoInitEnabled = enabled
    }
    
   @objc
   public static func getIsAutoInitEnabled() -> Bool {
       return Messaging.messaging().isAutoInitEnabled
   }
    
    @objc(register:completion:)
    public static func register(apnsToken: NSData, completion: @escaping (String?, NSError?) -> Void) {
        //Messaging.messaging().delegate = new FirebaseMessageDelegate();
        let data = Data(referencing: apnsToken);
        Messaging.messaging().apnsToken = data
        
        Messaging.messaging().token(completion: { fid, error in
            completion(fid, error as NSError?)
        })
    }
    
    @objc(unregister:)
    public static func unregister(completion: @escaping (NSError?) -> Void) {
        // need delegate to watch for fcmToken updates
        Messaging.messaging().deleteToken(completion: { error in
            completion(error as NSError?)
        })
    }
    
    @objc
    public static func getFcmToken() -> String? {
        return Messaging.messaging().fcmToken
    }
    
    @objc(subscribe:completion:)
    public static func subscribe(topic: String, completion: @escaping (NSError?) -> Void) {
        Messaging.messaging().subscribe(toTopic: topic, completion: { error in
            completion(error as NSError?)
        })
    }
    
    @objc(unsubscribe:completion:)
    public static func unsubscribe(topic: String, completion: @escaping (NSError?) -> Void) {
        Messaging.messaging().unsubscribe(fromTopic: topic, completion: { error in
            completion(error as NSError?)
        })
    }
}
