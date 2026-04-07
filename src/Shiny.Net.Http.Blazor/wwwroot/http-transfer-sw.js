// Shiny HTTP Transfer Service Worker
// This file can be used as a standalone service worker, or its handlers can be
// imported into your own service worker via:
//   importScripts('./_content/Shiny.Net.Http.Blazor/http-transfer-sw.js');
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

async function runTransfer(entry) {
    const init = {
        method: entry.method || 'GET',
        headers: entry.headers || {},
        credentials: 'include'
    };

    if (entry.body) {
        // body was stored as a Blob in IndexedDB (for raw uploads)
        init.body = entry.body;
    } else if (entry.formData) {
        // multipart — build from { fields: {k:v}, file: { name, blob } }
        const fd = new FormData();
        if (entry.formData.fields) {
            for (const [k, v] of Object.entries(entry.formData.fields)) fd.append(k, v);
        }
        if (entry.formData.file) {
            fd.append(entry.formData.file.name || 'file', entry.formData.file.blob, entry.formData.file.filename || 'file');
        }
        init.body = fd;
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

async function drainQueue() {
    const entries = await getAll();
    const pending = entries.filter(e => e.status === 'pending' || e.status === 'error');

    for (const entry of pending) {
        entry.status = 'in-progress';
        entry.updatedAt = Date.now();
        await put(entry);

        await broadcast({
            shinyHttp: true,
            type: 'status',
            identifier: entry.identifier,
            status: 'in-progress'
        });

        try {
            const result = await runTransfer(entry);

            entry.status = 'completed';
            entry.bytesTransferred = result.size || 0;
            entry.bytesToTransfer = result.size || null;
            entry.updatedAt = Date.now();
            entry.error = null;

            if (result.blob) {
                entry.resultBlob = result.blob;
            }

            await put(entry);
            await broadcast({
                shinyHttp: true,
                type: 'completed',
                identifier: entry.identifier
            });
        }
        catch (ex) {
            entry.status = 'error';
            entry.error = {
                message: ex && ex.message ? ex.message : String(ex),
                statusCode: (ex && ex.statusCode) || 0
            };
            entry.retryCount = (entry.retryCount || 0) + 1;
            entry.updatedAt = Date.now();
            await put(entry);

            await broadcast({
                shinyHttp: true,
                type: 'error',
                identifier: entry.identifier,
                message: entry.error.message,
                statusCode: entry.error.statusCode
            });

            // rethrow so the browser will retry the sync event later
            throw ex;
        }
    }
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
