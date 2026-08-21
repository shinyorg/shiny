import ActivityKit
import SwiftUI
import WidgetKit

// A data-driven Live Activity widget for Shiny.Mobile.LiveActivities.
//
// It renders whatever `LiveActivityContent` your .NET code (or your server) sends — title, body, short
// status and progress — so most apps never need to touch this file. Style it, or branch on
// `context.attributes.kind` to render different layouts per activity type.
//
// See README.md in this folder for how to add it to a .NET MAUI / .NET for iOS app.

@main
struct ShinyLiveActivityBundle: WidgetBundle {
    var body: some Widget {
        ShinyLiveActivityWidget()
    }
}


struct ShinyLiveActivityWidget: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: ShinyActivityAttributes.self) { context in
            // Lock Screen / notification-shade presentation.
            LockScreenView(state: context.state)
                .padding()
                .activityBackgroundTint(nil)
                .activitySystemActionForegroundColor(.primary)
        } dynamicIsland: { context in
            DynamicIsland {
                DynamicIslandExpandedRegion(.leading) {
                    if let title = context.state.title {
                        Text(title)
                            .font(.headline)
                            .lineLimit(1)
                    }
                }
                DynamicIslandExpandedRegion(.trailing) {
                    if let short = context.state.shortStatus {
                        Text(short)
                            .font(.headline)
                            .monospacedDigit()
                    }
                }
                DynamicIslandExpandedRegion(.bottom) {
                    VStack(alignment: .leading, spacing: 6) {
                        if let body = context.state.body {
                            Text(body)
                                .font(.caption)
                                .foregroundStyle(.secondary)
                        }
                        ProgressBar(state: context.state)
                    }
                }
            } compactLeading: {
                Image(systemName: "circle.dashed")
            } compactTrailing: {
                if let short = context.state.shortStatus {
                    Text(short).monospacedDigit()
                }
            } minimal: {
                Image(systemName: "circle.dashed")
            }
        }
    }
}


struct LockScreenView: View {
    let state: ShinyActivityAttributes.ContentState

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            if let title = state.title {
                HStack {
                    Text(title)
                        .font(.headline)
                    Spacer()
                    if let short = state.shortStatus {
                        Text(short)
                            .font(.headline)
                            .monospacedDigit()
                            .foregroundStyle(.secondary)
                    }
                }
            }

            if let body = state.body {
                Text(body)
                    .font(.subheadline)
                    .foregroundStyle(.secondary)
            }

            ProgressBar(state: state)
        }
    }
}


/// Renders whichever progress form the content carries: an explicit fraction, a system-animated time
/// range (preferred — it advances without further updates), or an indeterminate bar.
struct ProgressBar: View {
    let state: ShinyActivityAttributes.ContentState

    var body: some View {
        if let start = state.progressStart, let end = state.progressEnd, end > start {
            ProgressView(timerInterval: start...end, countsDown: false)
                .labelsHidden()
        } else if let progress = state.progress {
            ProgressView(value: min(max(progress, 0), 1))
                .labelsHidden()
        } else if state.indeterminate == true {
            ProgressView()
                .progressViewStyle(.linear)
        }
    }
}
