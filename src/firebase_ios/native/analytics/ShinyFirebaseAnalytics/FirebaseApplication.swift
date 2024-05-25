import Foundation
import FirebaseCore

@objc(FirebaseApplication)
public class FirebaseApplication : NSObject {
    
    @objc
    public static func autoConfigure() -> Bool {
        FirebaseApp.configure()
        return isConfigured()
    }
    
    @objc(configure:gcmSenderId:apiKey:projectId:)
    public static func configure(googleAppId: String, gcmSenderId: String, apiKey: String?, projectId: String?) -> Bool {
        let opt = FirebaseOptions(googleAppID: googleAppId, gcmSenderID: gcmSenderId)
        opt.apiKey = apiKey
        opt.projectID = projectId
        FirebaseApp.configure(options: opt)
        
        return isConfigured()
    }
    
    
    public static func isConfigured() -> Bool {
        return FirebaseApp.app() != nil
    }
}
