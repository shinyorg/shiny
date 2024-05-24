import Foundation
import FirebaseCore

@objc(FirebaseApplication)
public class FirebaseApplication : NSObject {
    
    @objc
    public static func autoConfigure() {
        FirebaseApp.configure()
    }
    
    @objc(configure:gcmSenderId:)
    public static func configure(googleAppId: String, gcmSenderId: String) {
        let opt = FirebaseOptions(googleAppID: googleAppId, gcmSenderID: gcmSenderId)
        FirebaseApp.configure(options: opt)
    }
}
