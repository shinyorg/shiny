// Shiny.Mobile.CallDirectory - the Call Directory extension's CallKit entry point. Compiled by Shiny.Mobile.CallDirectory.targets
// with swiftc together with ShinyCallDirectoryLoader.swift; there is no Xcode project.
import CallKit
import Foundation
import os

private let appliedGenerationKey = "ShinyCallDirectory.AppliedGeneration"
private let appliedSeqKey = "ShinyCallDirectory.AppliedSeq"
private let lastErrorKey = "ShinyCallDirectory.LastError"
private let log = Logger(subsystem: "Shiny.Mobile.CallDirectory", category: "Extension")

extension CXCallDirectoryExtensionContext: ShinyCallDirectorySink {
    func addBlocking(_ number: Int64) { addBlockingEntry(withNextSequentialPhoneNumber: number) }
    func addIdentification(_ number: Int64, _ label: String) { addIdentificationEntry(withNextSequentialPhoneNumber: number, label: label) }
    func removeBlocking(_ number: Int64) { removeBlockingEntry(withPhoneNumber: number) }
    func removeIdentification(_ number: Int64) { removeIdentificationEntry(withPhoneNumber: number) }
    func removeAll() {
        removeAllBlockingEntries()
        removeAllIdentificationEntries()
    }
}

@objc(ShinyCallDirectoryHandler)
final class ShinyCallDirectoryHandler: CXCallDirectoryProvider, CXCallDirectoryExtensionContextDelegate {

    override func beginRequest(with context: CXCallDirectoryExtensionContext) {
        context.delegate = self
        guard let group = Bundle.main.object(forInfoDictionaryKey: "ShinyCallDirectoryAppGroup") as? String,
              let defaults = UserDefaults(suiteName: group),
              let container = FileManager.default.containerURL(forSecurityApplicationGroupIdentifier: group) else {
            fail(context, nil, "the extension has no app group container - check ShinyCallDirectoryAppGroup and the extension's entitlements")
            return
        }
        defaults.removeObject(forKey: lastErrorKey)

        let applied = defaults.object(forKey: appliedGenerationKey) == nil ? nil : ShinyCallDirectoryApplied(
            generation: Int64(defaults.integer(forKey: appliedGenerationKey)),
            seq: Int64(defaults.integer(forKey: appliedSeqKey))
        )
        do {
            let result = try ShinyCallDirectoryLoader.load(
                path: container.appendingPathComponent("shiny_calldirectory.db").path,
                isIncremental: context.isIncremental,
                applied: applied,
                into: context
            )
            log.info("loaded (incremental: \(context.isIncremental)) generation \(result.generation) seq \(result.seq)")
            context.completeRequest { expired in
                // only a load the system kept describes what it holds
                if !expired {
                    defaults.set(result.generation, forKey: appliedGenerationKey)
                    defaults.set(result.seq, forKey: appliedSeqKey)
                }
            }
        } catch {
            fail(context, defaults, error.localizedDescription)
        }
    }


    private func fail(_ context: CXCallDirectoryExtensionContext, _ defaults: UserDefaults?, _ message: String) {
        log.error("load failed: \(message, privacy: .public)")
        defaults?.set(message, forKey: lastErrorKey)
        context.cancelRequest(withError: ShinyCallDirectoryError(message: message))
    }


    func requestFailed(for extensionContext: CXCallDirectoryExtensionContext, withError error: Error) {
        // the system rejected what was added (order, duplicates, size); the app reads this after a failed reload
        log.error("system rejected the load: \(error.localizedDescription, privacy: .public)")
        if let group = Bundle.main.object(forInfoDictionaryKey: "ShinyCallDirectoryAppGroup") as? String {
            UserDefaults(suiteName: group)?.set(error.localizedDescription, forKey: lastErrorKey)
        }
    }
}
