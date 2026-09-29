// Shiny.AppFunctions runtime: the Swift half of the bridge to .NET (Platforms/iOS/AppleBridge.cs is the other half).
// Compiled together with the generated intents into the app by Shiny.AppFunctions.targets.
import AppIntents
import Foundation

public typealias ShinyAFHandler = @convention(c) (
    UnsafePointer<CChar>,       // function id
    UnsafePointer<CChar>,       // arguments json
    Int32,                      // flags (1 = running in the foreground)
    UnsafeMutableRawPointer     // completion (a retained ShinyAFCompletion)
) -> Void

struct ShinyAFError: Error, CustomLocalizedStringResourceConvertible {
    let message: String
    var localizedStringResource: LocalizedStringResource { "\(message)" }
}

final class ShinyAFCompletion {
    let continuation: CheckedContinuation<(Int32, String), Never>
    init(_ continuation: CheckedContinuation<(Int32, String), Never>) { self.continuation = continuation }
}

/// The dispatcher's reply: {"value": …, "dialog": "…", "code": "…", "message": "…"}.
struct ShinyAFReply {
    let json: [String: Any]

    var value: Any? { json["value"] }

    /// Strings as-is, numbers and booleans as text, objects and lists as JSON text.
    var text: String {
        switch value {
        case nil, is NSNull: return ""
        case let s as String: return s
        case let n as NSNumber: return n.stringValue
        case let other?:
            guard JSONSerialization.isValidJSONObject(other),
                  let data = try? JSONSerialization.data(withJSONObject: other, options: [.sortedKeys]) else { return "\(other)" }
            return String(decoding: data, as: UTF8.self)
        }
    }
    var int: Int { (value as? NSNumber)?.intValue ?? Int(text) ?? 0 }
    var double: Double { (value as? NSNumber)?.doubleValue ?? Double(text) ?? 0 }
    var bool: Bool { (value as? NSNumber)?.boolValue ?? (text == "true") }
    var date: Date { ShinyAF.parseDate(text) ?? Date(timeIntervalSince1970: 0) }

    func dialog(orDefault fallback: String) -> IntentDialog {
        let text = (json["dialog"] as? String) ?? fallback
        return IntentDialog("\(text.isEmpty ? "Done" : text)")
    }
}

struct ShinyAFEntityItem {
    let id: String
    let title: String
}

enum ShinyAF {
    static let statusSuccess: Int32 = 0
    static let statusError: Int32 = 1
    static let statusNeedsForeground: Int32 = 2

    private static let lock = NSLock()
    nonisolated(unsafe) private static var handler: ShinyAFHandler?

    static func setHandler(_ h: ShinyAFHandler) {
        lock.lock(); handler = h; lock.unlock()
    }

    private static func currentHandler() -> ShinyAFHandler? {
        lock.lock(); defer { lock.unlock() }
        return handler
    }

    /// On a cold launch from Siri, Shortcuts or Spotlight, perform() can run before .NET has built the host.
    static func waitForHandler(timeout: TimeInterval = 10) async throws -> ShinyAFHandler {
        let deadline = Date().addingTimeInterval(timeout)
        while true {
            if let h = currentHandler() { return h }
            if Date() > deadline { throw ShinyAFError(message: "The app did not finish starting in time") }
            try await Task.sleep(nanoseconds: 50_000_000)
        }
    }

    /// Runs an app function. If a delegate asks for the app (AppFunctionGate.OpenApp), continues in the
    /// foreground and runs it again with the foreground flag.
    static func invoke<I: AppIntent>(_ intent: I, _ functionId: String, _ args: [String: Any], foreground: Bool) async throws -> ShinyAFReply {
        let json = encode(args)
        var (status, reply) = try await call(functionId, json, flags: foreground ? 1 : 0)
        if status == statusNeedsForeground {
            try await continueInForeground(intent, message(reply, "Continue in the app"))
            (status, reply) = try await call(functionId, json, flags: 1)
        }
        guard status == statusSuccess else { throw ShinyAFError(message: message(reply, "Something went wrong")) }
        return reply
    }

    /// Entity queries: entity:{id}:{ids|search|suggested}
    static func entities(_ entityId: String, _ operation: String, _ args: [String: Any]) async throws -> [ShinyAFEntityItem] {
        let (status, reply) = try await call("entity:\(entityId):\(operation)", encode(args), flags: 0)
        guard status == statusSuccess else { throw ShinyAFError(message: message(reply, "Could not load")) }
        let list = reply.value as? [[String: Any]] ?? []
        return list.compactMap { item in
            guard let id = item["id"] as? String else { return nil }
            return ShinyAFEntityItem(id: id, title: item["title"] as? String ?? id)
        }
    }

    static func iso8601(_ date: Date) -> String {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return f.string(from: date)
    }

    static func parseDate(_ text: String) -> Date? {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        if let d = f.date(from: text) { return d }
        f.formatOptions = [.withInternetDateTime]
        return f.date(from: text)
    }

    private static func call(_ functionId: String, _ json: String, flags: Int32) async throws -> (Int32, ShinyAFReply) {
        let h = try await waitForHandler()
        let (status, text) = await withCheckedContinuation { (cont: CheckedContinuation<(Int32, String), Never>) in
            let ctx = Unmanaged.passRetained(ShinyAFCompletion(cont)).toOpaque()
            functionId.withCString { id in json.withCString { args in h(id, args, flags, ctx) } }
        }
        let obj = (try? JSONSerialization.jsonObject(with: Data(text.utf8), options: [.fragmentsAllowed])) as? [String: Any] ?? [:]
        return (status, ShinyAFReply(json: obj))
    }

    private static func continueInForeground<I: AppIntent>(_ intent: I, _ message: String) async throws {
        if #available(iOS 26.0, *) {
            try await intent.continueInForeground(IntentDialog("\(message)"))
        } else if #available(iOS 17.0, *), let continuable = intent as? any ForegroundContinuableIntent {
            try await continuable.requestToContinueInForeground(IntentDialog("\(message)"))
        } else {
            throw ShinyAFError(message: message)
        }
    }

    private static func message(_ reply: ShinyAFReply, _ fallback: String) -> String {
        (reply.json["message"] as? String) ?? fallback
    }

    private static func encode(_ args: [String: Any]) -> String {
        let data = (try? JSONSerialization.data(withJSONObject: args)) ?? Data("{}".utf8)
        return String(decoding: data, as: UTF8.self)
    }
}

@_cdecl("shiny_af_set_handler")
public func shiny_af_set_handler(_ h: ShinyAFHandler) {
    ShinyAF.setHandler(h)
}

@_cdecl("shiny_af_complete")
public func shiny_af_complete(_ ctx: UnsafeMutableRawPointer, _ status: Int32, _ json: UnsafePointer<CChar>) {
    let completion = Unmanaged<ShinyAFCompletion>.fromOpaque(ctx).takeRetainedValue()
    completion.continuation.resume(returning: (status, String(cString: json)))
}
