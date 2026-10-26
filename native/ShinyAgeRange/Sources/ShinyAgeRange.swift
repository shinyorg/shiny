import Foundation
import DeclaredAgeRange
#if os(iOS)
import UIKit
#else
import AppKit
#endif

// C-ABI bridge over Apple's Declared Age Range framework for Shiny.AgeSignals.
// DeclaredAgeRange is Swift-only, so .NET cannot bind it directly. Every async export completes by invoking the
// callback exactly once with UTF-8 JSON (result or error), synchronously inside withCString - the managed side copies
// the strings before returning, so nothing needs to be freed across the boundary.
//
// The framework is weak-linked (build.sh) and every call is behind #available, so the library loads on iOS 15 /
// macOS 15 and reports "unsupported" there.

public typealias ShinyAgeRangeCallback = @convention(c) (UnsafeMutableRawPointer?, UnsafePointer<CChar>?, UnsafePointer<CChar>?) -> Void


struct CallbackContext: @unchecked Sendable {
    let raw: UnsafeMutableRawPointer?
    let callback: ShinyAgeRangeCallback

    func success(_ value: Any) {
        guard
            let data = try? JSONSerialization.data(withJSONObject: value, options: [.fragmentsAllowed]),
            let json = String(data: data, encoding: .utf8)
        else {
            self.failure(BridgeError(code: "unknown", message: "Failed to serialize the age range result"))
            return
        }
        json.withCString { self.callback(self.raw, $0, nil) }
    }

    func failure(_ error: Error) {
        let info = mapError(error)
        let dict: [String: Any] = ["code": info.code, "message": info.message]
        let data = (try? JSONSerialization.data(withJSONObject: dict)) ?? Data("{\"code\":\"unknown\"}".utf8)
        let json = String(data: data, encoding: .utf8) ?? "{\"code\":\"unknown\"}"
        json.withCString { self.callback(self.raw, nil, $0) }
    }
}


struct BridgeError: Error {
    let code: String
    let message: String
}


// MARK: - Exports

@_cdecl("shiny_agerange_is_supported")
public func shiny_agerange_is_supported() -> Bool {
    if #available(iOS 26.0, macOS 26.0, *) {
        return true
    }
    return false
}


/// {"isEligible": bool|null, "regulatoryFeatures": [string]|null} - null where the OS predates the property.
@_cdecl("shiny_agerange_eligibility")
public func shiny_agerange_eligibility(_ context: UnsafeMutableRawPointer?, _ callback: ShinyAgeRangeCallback) {
    let ctx = CallbackContext(raw: context, callback: callback)

    Task {
        var result: [String: Any] = [:]
        do {
            if #available(iOS 26.2, macOS 26.2, *) {
                result["isEligible"] = try await AgeRangeService.shared.isEligibleForAgeFeatures
            }
            if #available(iOS 26.4, macOS 26.4, *) {
                let features = try await AgeRangeService.shared.requiredRegulatoryFeatures
                result["regulatoryFeatures"] = features.map { String(describing: $0) }
            }
            ctx.success(result)
        } catch {
            ctx.failure(error)
        }
    }
}


/// Age gates are 1-3 ascending ages; 0 means "not set" for the second and third.
/// {"status": "shared"|"declined", "lowerBound", "upperBound", "declaration", "communicationLimits"}
@_cdecl("shiny_agerange_request")
public func shiny_agerange_request(_ gate1: Int32, _ gate2: Int32, _ gate3: Int32, _ context: UnsafeMutableRawPointer?, _ callback: ShinyAgeRangeCallback) {
    let ctx = CallbackContext(raw: context, callback: callback)

    guard #available(iOS 26.0, macOS 26.0, *) else {
        ctx.failure(BridgeError(code: "unsupported", message: "Declared Age Range needs iOS / macOS 26"))
        return
    }

    Task { @MainActor in
        do {
            let g2: Int? = gate2 > 0 ? Int(gate2) : nil
            let g3: Int? = gate3 > 0 ? Int(gate3) : nil

            #if os(iOS)
            guard let controller = topViewController() else {
                throw BridgeError(code: "noWindow", message: "There is no window to present the age range request from")
            }
            let response = try await AgeRangeService.shared.requestAgeRange(ageGates: Int(gate1), g2, g3, in: controller)
            #else
            guard let window = NSApplication.shared.keyWindow ?? NSApplication.shared.mainWindow else {
                throw BridgeError(code: "noWindow", message: "There is no window to present the age range request from")
            }
            let response = try await AgeRangeService.shared.requestAgeRange(ageGates: Int(gate1), g2, g3, in: window)
            #endif

            switch response {
            case .declinedSharing:
                ctx.success(["status": "declined"])

            case .sharing(let range):
                var result: [String: Any] = [
                    "status": "shared",
                    "communicationLimits": range.activeParentalControls.contains(.communicationLimits)
                ]
                if let lower = range.lowerBound {
                    result["lowerBound"] = lower
                }
                if let upper = range.upperBound {
                    result["upperBound"] = upper
                }
                // the case name, so cases added (or deprecated into "confirmed") by later SDKs map on the managed side
                if let declaration = range.ageRangeDeclaration {
                    result["declaration"] = String(describing: declaration)
                }
                ctx.success(result)

            @unknown default:
                throw BridgeError(code: "unknown", message: "Unknown age range response")
            }
        } catch {
            ctx.failure(error)
        }
    }
}


/// true when the acknowledgment was shown, false where the OS has no such sheet (macOS, iOS < 26.4)
@_cdecl("shiny_agerange_significant_update")
public func shiny_agerange_significant_update(_ description: UnsafePointer<CChar>, _ context: UnsafeMutableRawPointer?, _ callback: ShinyAgeRangeCallback) {
    let text = String(cString: description)
    let ctx = CallbackContext(raw: context, callback: callback)

    #if os(iOS)
    guard #available(iOS 26.4, *) else {
        ctx.success(false)
        return
    }

    Task { @MainActor in
        do {
            guard let scene = activeWindowScene() else {
                throw BridgeError(code: "noWindow", message: "There is no window scene to present the acknowledgment in")
            }
            try await AgeRangeService.shared.showSignificantUpdateAcknowledgment(in: scene, updateDescription: text)
            ctx.success(true)
        } catch {
            ctx.failure(error)
        }
    }
    #else
    _ = text
    ctx.success(false)
    #endif
}


// MARK: - Helpers

#if os(iOS)
@MainActor
func activeWindowScene() -> UIWindowScene? {
    let scenes = UIApplication.shared.connectedScenes.compactMap { $0 as? UIWindowScene }
    return scenes.first { $0.activationState == .foregroundActive } ?? scenes.first
}


@MainActor
func topViewController() -> UIViewController? {
    let window = activeWindowScene()?.windows.first { $0.isKeyWindow } ?? activeWindowScene()?.windows.first
    var controller = window?.rootViewController
    while let presented = controller?.presentedViewController {
        controller = presented
    }
    return controller
}
#endif


func mapError(_ error: Error) -> (code: String, message: String) {
    if let bridge = error as? BridgeError {
        return (bridge.code, bridge.message)
    }

    if #available(iOS 26.0, macOS 26.0, *), let service = error as? AgeRangeService.Error {
        switch service {
        case .notAvailable:
            return ("notAvailable", service.localizedDescription)
        case .invalidRequest:
            return ("invalidRequest", service.localizedDescription)
        default:
            // invalidAccount, declinedOnboarding and network only exist in the Xcode 27 SDK - naming them breaks the
            // build on Xcode 26, so read the case name instead (the enum has no payloads or custom description)
            let name = String(describing: service)
            let known = ["invalidAccount", "declinedOnboarding", "network"]
            return (known.contains(name) ? name : "unknown", service.localizedDescription)
        }
    }

    return ("unknown", error.localizedDescription)
}
