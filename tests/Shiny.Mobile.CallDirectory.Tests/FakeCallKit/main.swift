// Stands in for iOS in the call directory contract test: runs the extension's real loader against a store the C# side
// wrote, and enforces what CallKit enforces on a load - ascending additions, no duplicates, no removal of an entry the
// system does not hold. Built for macOS by CallDirectoryContractTests together with ShinyCallDirectoryLoader.swift.
//
//   fakecallkit <store.db> <state.json> [--full]
//
// state.json is what "iOS" holds between loads. No state file = the first load; --full = iOS asking for a full
// (non-incremental) load while keeping what was applied, as it does after a reset.
import Foundation

struct State: Codable {
    var blocked: [Int64] = []
    var identified: [String: String] = [:]
    var appliedGeneration: Int64?
    var appliedSeq: Int64?
}

struct Rejected: Error, CustomStringConvertible {
    let description: String
}

final class FakeContext: ShinyCallDirectorySink {
    var blocked: Set<Int64>
    var identified: [Int64: String]
    var lastBlockingAdd: Int64 = .min
    var lastIdentificationAdd: Int64 = .min
    var rejection: Rejected?

    init(_ state: State?) {
        blocked = Set(state?.blocked ?? [])
        identified = Dictionary(uniqueKeysWithValues: (state?.identified ?? [:]).map { (Int64($0.key)!, $0.value) })
    }

    private func reject(_ message: String) {
        if rejection == nil { rejection = Rejected(description: message) }
    }

    func addBlocking(_ number: Int64) {
        if number <= lastBlockingAdd { reject("entriesOutOfOrder: blocking \(number) after \(lastBlockingAdd)") }
        if blocked.contains(number) { reject("duplicateEntries: blocking \(number)") }
        lastBlockingAdd = number
        blocked.insert(number)
    }

    func addIdentification(_ number: Int64, _ label: String) {
        if number <= lastIdentificationAdd { reject("entriesOutOfOrder: identification \(number) after \(lastIdentificationAdd)") }
        if identified[number] != nil { reject("duplicateEntries: identification \(number)") }
        lastIdentificationAdd = number
        identified[number] = label
    }

    func removeBlocking(_ number: Int64) {
        if blocked.remove(number) == nil { reject("unexpectedIncrementalRemoval: blocking \(number)") }
    }

    func removeIdentification(_ number: Int64) {
        if identified.removeValue(forKey: number) == nil { reject("unexpectedIncrementalRemoval: identification \(number)") }
    }

    func removeAll() {
        blocked.removeAll()
        identified.removeAll()
    }
}

let args = CommandLine.arguments
guard args.count >= 3 else {
    FileHandle.standardError.write("usage: fakecallkit <store.db> <state.json> [--full]\n".data(using: .utf8)!)
    exit(2)
}
let statePath = args[2]
let previous = FileManager.default.fileExists(atPath: statePath)
    ? try JSONDecoder().decode(State.self, from: Data(contentsOf: URL(fileURLWithPath: statePath)))
    : nil
let incremental = previous != nil && !args.contains("--full")

// a full load starts from an empty system
let context = FakeContext(incremental ? previous : nil)
let applied = previous?.appliedGeneration.map { ShinyCallDirectoryApplied(generation: $0, seq: previous?.appliedSeq ?? 0) }

do {
    let result = try ShinyCallDirectoryLoader.load(path: args[1], isIncremental: incremental, applied: applied, into: context)
    if let rejection = context.rejection { throw rejection }

    let state = State(
        blocked: context.blocked.sorted(),
        identified: Dictionary(uniqueKeysWithValues: context.identified.map { (String($0.key), $0.value) }),
        appliedGeneration: result.generation,
        appliedSeq: result.seq
    )
    let encoder = JSONEncoder()
    encoder.outputFormatting = [.sortedKeys]
    try encoder.encode(state).write(to: URL(fileURLWithPath: statePath), options: .atomic)
    print(incremental ? "incremental" : "full")
} catch {
    // like CallKit: a rejected load leaves the system as it was
    FileHandle.standardError.write("\(error)\n".data(using: .utf8)!)
    exit(1)
}
