// Shiny HTTP Transfer Service Worker
// This file can be used as a standalone service worker, or its handlers can be
// imported into your own service worker via:
//   importScripts('./_content/Shiny.Net.Http/http-transfer-sw.js');
//
// It performs queued HTTP transfers via the Background Sync API while the
// Blazor WASM tab is closed or suspended.

const SHINY_HTTP_DB = 'shiny-http-transfers';
const SHINY_HTTP_STORE = 'transfers';
const SHINY_HTTP_SYNC_TAG = 'shiny-http-transfer';

function openDb() {
    return new Promise((resolve, reject) => {
        const req = indexedDB.open(SHINY_HTTP_DB, 1);
        req.onupgradeneeded = () => {
            const db = req.result;
            if (!db.objectStoreNames.contains(SHINY_HTTP_STORE)) {
                db.createObjectStore(SHINY_HTTP_STORE, { keyPath: 'identifier' });
            }
        };
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}

function tx(db, mode) {
    return db.transaction(SHINY_HTTP_STORE, mode).objectStore(SHINY_HTTP_STORE);
}

function promisify(req) {
    return new Promise((resolve, reject) => {
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}

async function getAll() {
    const db = await openDb();
    try { return await promisify(tx(db, 'readonly').getAll()); }
    finally { db.close(); }
}

async function get(id) {
    const db = await openDb();
    try { return await promisify(tx(db, 'readonly').get(id)); }
    finally { db.close(); }
}

async function put(entry) {
    const db = await openDb();
    try { await promisify(tx(db, 'readwrite').put(entry)); }
    finally { db.close(); }
}

async function remove(id) {
    const db = await openDb();
    try { await promisify(tx(db, 'readwrite').delete(id)); }
    finally { db.close(); }
}

async function broadcast(message) {
    const clientList = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
    for (const c of clientList) {
        c.postMessage(message);
    }
}

const SHINY_HTTP_MAX_RETRIES = 5;

async function runTransfer(entry) {
    const init = {
        method: entry.method || 'GET',
        headers: entry.headers || {},
        credentials: 'include'
    };

    if (entry.formData) {
        // multipart - the file's bytes are the entry's stored body; checking body first sent every
        // multipart upload as a raw body with no form fields
        const fd = new FormData();
        if (entry.formData.fields) {
            for (const [k, v] of Object.entries(entry.formData.fields)) fd.append(k, v);
        }
        const file = entry.formData.file;
        const fileBlob = entry.body || (file && file.blob);
        if (file && fileBlob) {
            fd.append(file.name || 'file', fileBlob, file.filename || 'file');
        }
        init.body = fd;
    } else if (entry.body) {
        // body was stored as a Blob in IndexedDB (for raw uploads)
        init.body = entry.body;
    }

    const response = await fetch(entry.uri, init);
    if (!response.ok) {
        const text = await response.text().catch(() => '');
        throw Object.assign(new Error(`HTTP ${response.status} ${response.statusText}`), {
            statusCode: response.status,
            body: text
        });
    }

    // For downloads, capture the response Blob and store it in IndexedDB
    // under `resultBlob`. The C# layer will read it back when the app reopens.
    if (entry.type === 'download') {
        const blob = await response.blob();
        return { blob, size: blob.size };
    }

    return { size: 0 };
}

// no status (network failure), timeouts, throttling and server errors are worth another try;
// any other 4xx will fail the same way again
function isTransient(statusCode) {
    return !statusCode || statusCode === 408 || statusCode === 429 || statusCode >= 500;
}

// Runs one transfer. Returns true when it failed in a way the browser should retry later.
async function processEntry(identifier) {
    // re-read - the entry may have been cancelled or paused since the drain listed it
    const entry = await get(identifier);
    if (!entry || (entry.status !== 'pending' && entry.status !== 'in-progress')) return false;

    entry.status = 'in-progress';
    entry.updatedAt = Date.now();
    await put(entry);

    await broadcast({
        shinyHttp: true,
        type: 'status',
        identifier: entry.identifier,
        status: 'in-progress'
    });

    let result;
    let failure;
    try {
        result = await runTransfer(entry);
    }
    catch (ex) {
        failure = ex;
    }

    // re-read again before writing back - writing the copy read above resurrected a transfer that was
    // cancelled while its request was in flight
    const current = await get(identifier);
    if (!current) return false;

    if (!failure) {
        current.status = 'completed';
        current.bytesTransferred = result.size || 0;
        current.bytesToTransfer = result.size || null;
        current.updatedAt = Date.now();
        current.error = null;

        if (result.blob) {
            current.resultBlob = result.blob;
        }

        await put(current);
        await broadcast({
            shinyHttp: true,
            type: 'completed',
            identifier: current.identifier
        });
        return false;
    }

    const statusCode = (failure && failure.statusCode) || 0;
    current.retryCount = (current.retryCount || 0) + 1;
    current.updatedAt = Date.now();
    current.error = {
        message: failure && failure.message ? failure.message : String(failure),
        statusCode
    };

    const retry = isTransient(statusCode) && current.retryCount < SHINY_HTTP_MAX_RETRIES;
    if (retry) {
        // back to pending (unless paused meanwhile) - reported as an error only once retries are exhausted,
        // so a transfer no longer fires OnError and later OnCompleted
        if (current.status !== 'paused') current.status = 'pending';
        await put(current);
        await broadcast({
            shinyHttp: true,
            type: 'status',
            identifier: current.identifier,
            status: current.status
        });
        return true;
    }

    current.status = 'error';
    await put(current);
    await broadcast({
        shinyHttp: true,
        type: 'error',
        identifier: current.identifier,
        message: current.error.message,
        statusCode
    });
    return false;
}

async function drainOnce() {
    // Drains run one at a time (see drainQueue), so an 'in-progress' entry seen here belongs to a worker
    // that was stopped mid-request - it would otherwise stay 'in-progress' forever.
    const entries = await getAll();
    const ids = entries
        .filter(e => e.status === 'pending' || e.status === 'in-progress')
        .map(e => e.identifier);

    let retryLater = false;
    for (const id of ids) {
        if (await processEntry(id)) retryLater = true;
    }

    if (retryLater) {
        // rethrow so the browser will retry the sync event later
        throw new Error('One or more transfers will be retried');
    }
}

// The sync event and the page's 'drain' message both arrive for the same queue() call. Running them
// side by side sent every transfer twice, so drains are serialised: a request that arrives while one is
// running asks it to make another pass instead.
let drainPromise = null;
let drainAgain = false;

function drainQueue() {
    if (drainPromise) {
        drainAgain = true;
        return drainPromise;
    }

    drainPromise = (async () => {
        try {
            let lastError = null;
            do {
                drainAgain = false;
                try {
                    await drainOnce();
                    lastError = null;
                } catch (ex) {
                    lastError = ex;
                }
            } while (drainAgain);

            if (lastError) throw lastError;
        } finally {
            drainPromise = null;
        }
    })();
    return drainPromise;
}

self.addEventListener('sync', event => {
    if (event.tag === SHINY_HTTP_SYNC_TAG) {
        event.waitUntil(drainQueue());
    }
});

// Also listen for periodicsync if the user has granted it — not widely supported.
self.addEventListener('periodicsync', event => {
    if (event.tag === SHINY_HTTP_SYNC_TAG) {
        event.waitUntil(drainQueue());
    }
});

// Allow foreground clients to kick a drain immediately after queueing.
self.addEventListener('message', event => {
    if (event.data && event.data.shinyHttp && event.data.type === 'drain') {
        event.waitUntil(drainQueue().catch(() => { /* retried by sync */ }));
    }
});
