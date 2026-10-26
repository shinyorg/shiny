import Foundation
import UniformTypeIdentifiers
import UserNotifications

// Shiny.Push stock Notification Service Extension.
//
// iOS hands every alert push carrying "mutable-content": 1 to this extension before showing it. It looks for an
// image URL in the payload, downloads it and attaches it to the notification. Whatever goes wrong (no URL, a failed
// or oversized download, an unsupported type, the ~30 second budget running out) the original notification is
// still delivered - an extension that never calls the content handler makes iOS drop the push's rich content.
//
// Configured from the extension's Info.plist, which Shiny.Push.targets writes from MSBuild properties:
//   ShinyPushImageKeys      array of payload keys checked in order; a dotted key walks nested dictionaries
//   ShinyPushImageMaxBytes  largest download attached (iOS's own limits: 10 MB images, 5 MB audio, 50 MB video)
@objc(ShinyNotificationService)
class ShinyNotificationService: UNNotificationServiceExtension {

    static let defaultKeys = ["image", "image-url", "imageUrl", "attachment-url", "fcm_options.image"]

    private let lock = NSLock()
    private var contentHandler: ((UNNotificationContent) -> Void)?
    private var content: UNMutableNotificationContent?
    private var task: URLSessionDownloadTask?

    override func didReceive(_ request: UNNotificationRequest, withContentHandler contentHandler: @escaping (UNNotificationContent) -> Void) {
        guard let content = request.content.mutableCopy() as? UNMutableNotificationContent else {
            contentHandler(request.content)
            return
        }
        self.contentHandler = contentHandler
        self.content = content

        guard let url = Self.imageUrl(in: content.userInfo, keys: Self.configuredKeys()) else {
            self.deliver()
            return
        }

        let maxBytes = Self.configuredMaxBytes()
        let task = URLSession.shared.downloadTask(with: url) { [weak self] location, response, error in
            guard let self = self else { return }
            if let location = location, error == nil,
               let attachment = Self.attachment(from: location, response: response, url: url, maxBytes: maxBytes) {
                self.lock.lock()
                self.content?.attachments.append(attachment)
                self.lock.unlock()
            }
            self.deliver()
        }
        self.task = task
        task.resume()
    }

    override func serviceExtensionTimeWillExpire() {
        self.task?.cancel()
        self.deliver()
    }

    // called from the download callback and the expiry, in either order - only the first one reaches iOS
    private func deliver() {
        self.lock.lock()
        let handler = self.contentHandler
        let content = self.content
        self.contentHandler = nil
        self.lock.unlock()

        if let handler = handler, let content = content {
            handler(content)
        }
    }

    static func configuredKeys() -> [String] {
        if let keys = Bundle.main.object(forInfoDictionaryKey: "ShinyPushImageKeys") as? [String], !keys.isEmpty {
            return keys
        }
        return defaultKeys
    }

    static func configuredMaxBytes() -> Int64 {
        if let value = Bundle.main.object(forInfoDictionaryKey: "ShinyPushImageMaxBytes") as? NSNumber, value.int64Value > 0 {
            return value.int64Value
        }
        return 10 * 1024 * 1024
    }

    static func imageUrl(in userInfo: [AnyHashable: Any], keys: [String]) -> URL? {
        for key in keys {
            if let value = lookup(userInfo, path: key.split(separator: ".").map(String.init)),
               let url = URL(string: value.trimmingCharacters(in: .whitespacesAndNewlines)),
               let scheme = url.scheme?.lowercased(), scheme == "https" || scheme == "http" {
                return url
            }
        }
        return nil
    }

    private static func lookup(_ dictionary: [AnyHashable: Any], path: [String]) -> String? {
        guard let first = path.first, let value = dictionary[first] else { return nil }
        if path.count == 1 {
            return value as? String
        }
        guard let nested = value as? [AnyHashable: Any] else { return nil }
        return lookup(nested, path: Array(path.dropFirst()))
    }

    static func attachment(from location: URL, response: URLResponse?, url: URL, maxBytes: Int64) -> UNNotificationAttachment? {
        if let http = response as? HTTPURLResponse, !(200...299).contains(http.statusCode) {
            return nil
        }
        let size = (try? FileManager.default.attributesOfItem(atPath: location.path)[.size] as? NSNumber)?.int64Value ?? 0
        if size == 0 || size > maxBytes {
            return nil
        }

        // iOS picks the attachment type from the file extension, and the downloaded temp file has none
        var type: UTType? = nil
        if let mime = response?.mimeType {
            type = UTType(mimeType: mime)
        }
        if type == nil || type == .data || type == .item, !url.pathExtension.isEmpty {
            type = UTType(filenameExtension: url.pathExtension)
        }
        guard let resolved = type, let ext = resolved.preferredFilenameExtension else {
            return nil
        }

        let file = FileManager.default.temporaryDirectory
            .appendingPathComponent(UUID().uuidString)
            .appendingPathExtension(ext)
        do {
            try FileManager.default.moveItem(at: location, to: file)
            return try UNNotificationAttachment(
                identifier: "shiny-push-image",
                url: file,
                options: [UNNotificationAttachmentOptionsTypeHintKey: resolved.identifier]
            )
        } catch {
            try? FileManager.default.removeItem(at: file)
            return nil
        }
    }
}
