import Foundation

#if canImport(ActivityKit)
import ActivityKit
#endif

/// Objective-C facing wrapper over ActivityKit, which is a Swift-only framework and therefore
/// unreachable from .NET without a shim like this one.
///
/// Everything crossing the boundary is a JSON string or a primitive so the binding stays trivial and the
/// managed side keeps full control of the payload shape (see `LiveActivityContentSchema` in
/// Shiny.Mobile.LiveActivities).
@objc(ShinyActivityBridge)
public class ShinyActivityBridge: NSObject {

    private static var isObserving = false
    private static var tokenHandler: ((String, String) -> Void)?
    private static var pushToStartHandler: ((String) -> Void)?
    private static var stateHandler: ((String, String) -> Void)?
    private static var startedHandler: ((String) -> Void)?
    private static var cachedPushToStartToken: String?

    // MARK: - capability

    /// Whether this OS build has ActivityKit at all.
    @objc public static func isSupported() -> Bool {
        if #available(iOS 16.2, *) {
            return true
        }
        return false
    }

    /// Whether the user has live activities enabled for this app.
    @objc public static func areActivitiesEnabled() -> Bool {
        #if canImport(ActivityKit)
        if #available(iOS 16.2, *) {
            return ActivityAuthorizationInfo().areActivitiesEnabled
        }
        #endif
        return false
    }

    // MARK: - lifecycle

    /// Starts an activity. `attributesJson`/`contentStateJson` are the shapes documented by
    /// `LiveActivityContentSchema`. Returns the new activity's id.
    @objc(startWithAttributes:contentState:staleDate:relevanceScore:requestPushToken:error:)
    public static func start(
        attributes attributesJson: String,
        contentState contentStateJson: String,
        staleDate: NSNumber?,
        relevanceScore: NSNumber?,
        requestPushToken: Bool
    ) throws -> String {
        #if canImport(ActivityKit)
        guard #available(iOS 16.2, *) else {
            throw self.error("Live Activities require iOS 16.2 or later")
        }
        guard ActivityAuthorizationInfo().areActivitiesEnabled else {
            throw self.error("Live Activities are disabled for this app in Settings")
        }

        let attributes = try self.decodeAttributes(attributesJson)
        let state = try self.decodeState(contentStateJson)

        let content = ActivityContent(
            state: state,
            staleDate: staleDate.map { Date(timeIntervalSince1970: $0.doubleValue) },
            relevanceScore: relevanceScore?.doubleValue ?? 0
        )

        let activity = try Activity.request(
            attributes: attributes,
            content: content,
            pushType: requestPushToken ? .token : nil
        )

        self.observe(activity)
        return activity.id
        #else
        throw self.error("ActivityKit is not available on this platform")
        #endif
    }


    /// Replaces a running activity's content. Pass alert text to make the update alerting.
    @objc(updateWithId:contentState:staleDate:relevanceScore:alertTitle:alertBody:completion:)
    public static func update(
        id: String,
        contentState contentStateJson: String,
        staleDate: NSNumber?,
        relevanceScore: NSNumber?,
        alertTitle: String?,
        alertBody: String?,
        completion: @escaping (NSError?) -> Void
    ) {
        #if canImport(ActivityKit)
        guard #available(iOS 16.2, *) else {
            completion(self.error("Live Activities require iOS 16.2 or later"))
            return
        }
        guard let activity = self.find(id) else {
            completion(self.error("No live activity found with id \(id)"))
            return
        }

        do {
            let state = try self.decodeState(contentStateJson)
            let content = ActivityContent(
                state: state,
                staleDate: staleDate.map { Date(timeIntervalSince1970: $0.doubleValue) },
                relevanceScore: relevanceScore?.doubleValue ?? 0
            )

            var alert: AlertConfiguration?
            if let title = alertTitle {
                alert = AlertConfiguration(
                    title: LocalizedStringResource(stringLiteral: title),
                    body: LocalizedStringResource(stringLiteral: alertBody ?? ""),
                    sound: .default
                )
            }

            Task {
                await activity.update(content, alertConfiguration: alert)
                completion(nil)
            }
        } catch {
            completion(error as NSError)
        }
        #else
        completion(self.error("ActivityKit is not available on this platform"))
        #endif
    }


    /// Ends an activity, optionally with a final state and a dismissal time (Unix seconds).
    @objc(endWithId:contentState:dismissAt:completion:)
    public static func end(
        id: String,
        contentState contentStateJson: String?,
        dismissAt: NSNumber?,
        completion: @escaping (NSError?) -> Void
    ) {
        #if canImport(ActivityKit)
        guard #available(iOS 16.2, *) else {
            completion(self.error("Live Activities require iOS 16.2 or later"))
            return
        }
        guard let activity = self.find(id) else {
            // Already gone — ending twice is not an error worth surfacing.
            completion(nil)
            return
        }

        var content: ActivityContent<ShinyActivityAttributes.ContentState>?
        if let json = contentStateJson {
            do {
                content = ActivityContent(state: try self.decodeState(json), staleDate: nil)
            } catch {
                completion(error as NSError)
                return
            }
        }

        let policy: ActivityUIDismissalPolicy
        if let dismissAt = dismissAt {
            policy = .after(Date(timeIntervalSince1970: dismissAt.doubleValue))
        } else {
            policy = .default
        }

        Task {
            await activity.end(content, dismissalPolicy: policy)
            completion(nil)
        }
        #else
        completion(self.error("ActivityKit is not available on this platform"))
        #endif
    }


    /// Ends every activity this app has running.
    @objc(endAllWithCompletion:)
    public static func endAll(completion: @escaping () -> Void) {
        #if canImport(ActivityKit)
        guard #available(iOS 16.2, *) else {
            completion()
            return
        }

        Task {
            for activity in Activity<ShinyActivityAttributes>.activities {
                await activity.end(nil, dismissalPolicy: .immediate)
            }
            completion()
        }
        #else
        completion()
        #endif
    }


    /// Every running activity as `["id": …, "state": …, "pushToken": …]`.
    @objc public static func activeActivities() -> [[String: String]] {
        #if canImport(ActivityKit)
        if #available(iOS 16.2, *) {
            return Activity<ShinyActivityAttributes>.activities.map { activity in
                var dict = ["id": activity.id, "state": self.stateName(activity.activityState)]
                if let token = activity.pushToken {
                    dict["pushToken"] = self.hex(token)
                }
                return dict
            }
        }
        #endif
        return []
    }


    /// The device's push-to-start token (iOS 17.2+), if one has been issued this session.
    @objc public static func pushToStartToken() -> String? {
        return self.cachedPushToStartToken
    }

    // MARK: - observation

    /// Begins observing activity lifecycle, per-activity push tokens and (17.2+) the push-to-start
    /// token. Call once at startup — activities survive app restarts, so this is how the managed side
    /// learns about activities and tokens it never saw created.
    @objc(startObservingWithStarted:token:pushToStart:state:)
    public static func startObserving(
        started: @escaping (String) -> Void,
        token: @escaping (String, String) -> Void,
        pushToStart: @escaping (String) -> Void,
        state: @escaping (String, String) -> Void
    ) {
        self.startedHandler = started
        self.tokenHandler = token
        self.pushToStartHandler = pushToStart
        self.stateHandler = state

        guard !self.isObserving else { return }
        self.isObserving = true

        #if canImport(ActivityKit)
        guard #available(iOS 16.2, *) else { return }

        // Activities that already exist (app relaunch, or one started by a push).
        for activity in Activity<ShinyActivityAttributes>.activities {
            self.observe(activity)
        }

        // Activities started later, including by a push-to-start payload while we were closed.
        Task {
            for await activity in Activity<ShinyActivityAttributes>.activityUpdates {
                self.startedHandler?(activity.id)
                self.observe(activity)
            }
        }

        if #available(iOS 17.2, *) {
            Task {
                for await tokenData in Activity<ShinyActivityAttributes>.pushToStartTokenUpdates {
                    let value = self.hex(tokenData)
                    self.cachedPushToStartToken = value
                    self.pushToStartHandler?(value)
                }
            }
        }
        #endif
    }

    // MARK: - internals

    #if canImport(ActivityKit)
    @available(iOS 16.2, *)
    private static func find(_ id: String) -> Activity<ShinyActivityAttributes>? {
        return Activity<ShinyActivityAttributes>.activities.first(where: { $0.id == id })
    }


    @available(iOS 16.2, *)
    private static func observe(_ activity: Activity<ShinyActivityAttributes>) {
        Task {
            for await tokenData in activity.pushTokenUpdates {
                self.tokenHandler?(activity.id, self.hex(tokenData))
            }
        }
        Task {
            for await state in activity.activityStateUpdates {
                self.stateHandler?(activity.id, self.stateName(state))
            }
        }
    }


    @available(iOS 16.2, *)
    private static func stateName(_ state: ActivityState) -> String {
        switch state {
        case .active: return "active"
        case .ended: return "ended"
        case .dismissed: return "dismissed"
        case .stale: return "stale"
        // Plain default (not @unknown default): ActivityState gains cases across OS versions and the
        // managed side only models the four above.
        default: return "active"
        }
    }


    @available(iOS 16.1, *)
    private static func decodeAttributes(_ json: String) throws -> ShinyActivityAttributes {
        guard let data = json.data(using: .utf8) else {
            throw self.error("Attributes JSON was not valid UTF-8")
        }
        return try JSONDecoder().decode(ShinyActivityAttributes.self, from: data)
    }


    @available(iOS 16.1, *)
    private static func decodeState(_ json: String) throws -> ShinyActivityAttributes.ContentState {
        guard let data = json.data(using: .utf8) else {
            throw self.error("Content state JSON was not valid UTF-8")
        }
        return try JSONDecoder().decode(ShinyActivityAttributes.ContentState.self, from: data)
    }
    #endif


    private static func hex(_ data: Data) -> String {
        return data.map { String(format: "%02x", $0) }.joined()
    }


    private static func error(_ message: String) -> NSError {
        return NSError(
            domain: "ShinyLiveActivities",
            code: 1,
            userInfo: [NSLocalizedDescriptionKey: message]
        )
    }
}
