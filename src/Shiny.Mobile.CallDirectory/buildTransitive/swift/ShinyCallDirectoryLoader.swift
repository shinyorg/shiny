// Shiny.Mobile.CallDirectory - what the Call Directory extension loads, with no CallKit in it, so the same code runs in the
// extension and in the macOS contract test (tests/Shiny.Mobile.CallDirectory.Tests). It only ever READS the app's SQLite
// store, so the app is the one writer.
//
// The schema is a contract with Infrastructure/CallDirectoryStore.cs - change one, change both, bump the version:
//   meta    (key TEXT PRIMARY KEY, value TEXT)                       schema_version, generation
//   entries (number INTEGER PRIMARY KEY, label TEXT, type INTEGER)   type 0 = block, 1 = identify, 2 = allow
//   changes (seq INTEGER PRIMARY KEY AUTOINCREMENT, number INTEGER, old_type INTEGER)   old_type NULL = not stored
//
// What the system already holds is described by the generation + change seq last applied, which the extension saves
// to the app group's UserDefaults once the system accepts a load. The app trims applied change rows.
import Foundation
import SQLite3

let shinyCallDirectorySchemaVersion: Int64 = 1

/// The calls a load makes - CXCallDirectoryExtensionContext in the extension, a fake in the contract test.
protocol ShinyCallDirectorySink {
    func addBlocking(_ number: Int64)
    func addIdentification(_ number: Int64, _ label: String)
    func removeBlocking(_ number: Int64)
    func removeIdentification(_ number: Int64)
    func removeAll()
}

/// The generation and change seq a load brought the system to.
struct ShinyCallDirectoryApplied: Equatable {
    let generation: Int64
    let seq: Int64
}

struct ShinyCallDirectoryError: LocalizedError {
    let message: String
    var errorDescription: String? { message }
}

// iOS has no allow list - an allowed number is loaded as an identification entry
private enum Kind { case block, identify, none }
private func kind(_ type: Int64?) -> Kind {
    switch type {
    case .none: return .none
    case .some(0): return .block
    default: return .identify
    }
}

enum ShinyCallDirectoryLoader {

    /// - Parameters:
    ///   - path: the store; a missing file is an empty directory
    ///   - isIncremental: the system kept what it had (CXCallDirectoryExtensionContext.isIncremental)
    ///   - applied: what the last accepted load applied, nil when unknown
    static func load(path: String, isIncremental: Bool, applied: ShinyCallDirectoryApplied?, into sink: ShinyCallDirectorySink) throws -> ShinyCallDirectoryApplied {
        guard FileManager.default.fileExists(atPath: path) else {
            // the app has never written a number
            if isIncremental { sink.removeAll() }
            return ShinyCallDirectoryApplied(generation: 0, seq: 0)
        }

        let db = try Database(path: path)
        defer { db.close() }

        // one read transaction, so the removal and addition passes see the same snapshot
        try db.exec("BEGIN")
        defer { try? db.exec("COMMIT") }

        let version = try db.scalar("SELECT CAST(value AS INTEGER) FROM meta WHERE key = 'schema_version'") ?? 0
        guard version == shinyCallDirectorySchemaVersion else {
            throw ShinyCallDirectoryError(message: "store schema \(version), extension reads \(shinyCallDirectorySchemaVersion)")
        }
        let generation = try db.scalar("SELECT CAST(value AS INTEGER) FROM meta WHERE key = 'generation'") ?? 0
        let seq = try db.scalar("SELECT seq FROM sqlite_sequence WHERE name = 'changes'") ?? 0

        if isIncremental, let applied, applied.generation == generation {
            try loadChanges(db, sink, from: applied.seq, to: seq)
        } else {
            if isIncremental { sink.removeAll() }
            try loadAll(db, sink)
        }
        return ShinyCallDirectoryApplied(generation: generation, seq: seq)
    }


    // full load: one ordered pass satisfies the ascending-order rule for both lists
    private static func loadAll(_ db: Database, _ sink: ShinyCallDirectorySink) throws {
        try db.each("SELECT number, type, label FROM entries ORDER BY number") { row in
            if kind(row.int64(1)) == .block {
                sink.addBlocking(row.int64(0))
            } else {
                sink.addIdentification(row.int64(0), row.text(2) ?? "")
            }
        }
    }


    // incremental load: removals first, then additions, each in number order
    private static func loadChanges(_ db: Database, _ sink: ShinyCallDirectorySink, from: Int64, to: Int64) throws {
        // the FIRST change after `from` carries what the system holds; entries carries what it should hold
        let sql = """
            SELECT c.number, c.old_type, e.type, e.label
            FROM (SELECT number, old_type, MIN(seq) AS s FROM changes WHERE seq > ?1 AND seq <= ?2 GROUP BY number) c
            LEFT JOIN entries e ON e.number = c.number
            ORDER BY c.number
            """
        try db.each(sql, from, to) { row in
            let old = kind(row.isNull(1) ? nil : row.int64(1))
            let new = kind(row.isNull(2) ? nil : row.int64(2))
            if old == .block && new != .block {
                sink.removeBlocking(row.int64(0))
            }
            // a label change is a remove + add
            if old == .identify {
                sink.removeIdentification(row.int64(0))
            }
        }
        try db.each(sql, from, to) { row in
            let old = kind(row.isNull(1) ? nil : row.int64(1))
            let new = kind(row.isNull(2) ? nil : row.int64(2))
            if new == .block && old != .block {
                sink.addBlocking(row.int64(0))
            } else if new == .identify {
                sink.addIdentification(row.int64(0), row.text(3) ?? "")
            }
        }
    }
}


// MARK: - the few SQLite calls the loader needs

private final class Database {
    private var handle: OpaquePointer?

    init(path: String) throws {
        // read-write (never create) only so SQLite can roll back a hot journal the app left behind; nothing is written
        guard sqlite3_open_v2(path, &handle, SQLITE_OPEN_READWRITE, nil) == SQLITE_OK else {
            let message = handle.map { String(cString: sqlite3_errmsg($0)) } ?? "open failed"
            sqlite3_close_v2(handle)
            throw ShinyCallDirectoryError(message: "could not open \(path): \(message)")
        }
        sqlite3_busy_timeout(handle, 5000)
        // the extension's memory ceiling is small: keep the page cache tiny, rows stream through a cursor
        try exec("PRAGMA cache_size = -256")
    }

    func close() {
        sqlite3_close_v2(handle)
        handle = nil
    }

    func exec(_ sql: String) throws {
        guard sqlite3_exec(handle, sql, nil, nil, nil) == SQLITE_OK else { throw error(sql) }
    }

    func scalar(_ sql: String) throws -> Int64? {
        var value: Int64?
        try each(sql) { value = $0.int64(0) }
        return value
    }

    func each(_ sql: String, _ args: Int64..., row: (Row) throws -> Void) throws {
        var stmt: OpaquePointer?
        guard sqlite3_prepare_v2(handle, sql, -1, &stmt, nil) == SQLITE_OK else { throw error(sql) }
        defer { sqlite3_finalize(stmt) }
        for (i, arg) in args.enumerated() {
            sqlite3_bind_int64(stmt, Int32(i + 1), arg)
        }
        let r = Row(stmt: stmt)
        while true {
            let rc = sqlite3_step(stmt)
            if rc == SQLITE_DONE { break }
            guard rc == SQLITE_ROW else { throw error(sql) }
            // the bridged label strings are autoreleased; drain them so a large list stays inside the memory limit
            try autoreleasepool { try row(r) }
        }
    }

    private func error(_ sql: String) -> ShinyCallDirectoryError {
        ShinyCallDirectoryError(message: "\(String(cString: sqlite3_errmsg(handle))) - \(sql)")
    }
}

private struct Row {
    let stmt: OpaquePointer?
    func int64(_ col: Int32) -> Int64 { sqlite3_column_int64(stmt, col) }
    func isNull(_ col: Int32) -> Bool { sqlite3_column_type(stmt, col) == SQLITE_NULL }
    func text(_ col: Int32) -> String? { sqlite3_column_text(stmt, col).map { String(cString: $0) } }
}
