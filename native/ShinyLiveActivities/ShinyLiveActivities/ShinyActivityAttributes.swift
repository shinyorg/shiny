import Foundation

#if canImport(ActivityKit)
import ActivityKit

/// The single `ActivityAttributes` type every Shiny live activity uses.
///
/// ActivityKit needs a concrete `Codable` type at compile time, which would normally force every app to
/// hand-write its own — and would make a general purpose .NET API impossible. Instead this one type is
/// shared: the fields that matter to the system are strongly typed, and anything app-specific rides in
/// the `data`/`values` string dictionaries.
///
/// Your widget extension links this framework and renders it:
/// ```swift
/// ActivityConfiguration(for: ShinyActivityAttributes.self) { context in
///     VStack {
///         Text(context.state.title ?? "")
///         if let progress = context.state.progress {
///             ProgressView(value: progress)
///         }
///     }
/// }
/// ```
///
/// The name `ShinyActivityAttributes` is also what a server sends as `attributes-type` in a
/// push-to-start payload.
@available(iOS 16.1, *)
public struct ShinyActivityAttributes: ActivityAttributes {

    /// The part of the activity that changes over its lifetime. Mirrors `Shiny.LiveActivities.LiveActivityContent`
    /// and the `content-state` a server pushes — see `LiveActivityContentSchema` on the .NET side.
    public struct ContentState: Codable, Hashable {
        /// The headline.
        public var title: String?
        /// Supporting detail.
        public var body: String?
        /// A very short status for the Dynamic Island compact view.
        public var shortStatus: String?
        /// Completed fraction, 0.0 - 1.0.
        public var progress: Double?
        /// Start of a system-animated time range.
        public var progressStart: Date?
        /// End of a system-animated time range.
        public var progressEnd: Date?
        /// Whether progress is indeterminate.
        public var indeterminate: Bool?
        /// App-specific values.
        public var data: [String: String]?

        public init(
            title: String? = nil,
            body: String? = nil,
            shortStatus: String? = nil,
            progress: Double? = nil,
            progressStart: Date? = nil,
            progressEnd: Date? = nil,
            indeterminate: Bool? = nil,
            data: [String: String]? = nil
        ) {
            self.title = title
            self.body = body
            self.shortStatus = shortStatus
            self.progress = progress
            self.progressStart = progressStart
            self.progressEnd = progressEnd
            self.indeterminate = indeterminate
            self.data = data
        }
    }

    /// Selects which widget/layout renders this activity, for apps shipping more than one.
    public var kind: String?

    /// Static values that never change for this activity's lifetime.
    public var values: [String: String]

    public init(kind: String? = nil, values: [String: String] = [:]) {
        self.kind = kind
        self.values = values
    }
}
#endif
