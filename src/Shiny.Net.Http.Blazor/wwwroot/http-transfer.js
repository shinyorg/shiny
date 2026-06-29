// Foreground JS interop module for Shiny.Net.Http.Blazor
// Backed by IndexedDB; relies on http-transfer-sw.js (registered by the host)
// to drain the queue via the Background Sync API.

const SHINY_HTTP_DB = 'shiny-http-transfers';
const SHINY_HTTP_STORE = 'transfers';
const SHINY_HTTP_SYNC_TAG = 'shiny-http-transfer';

let dotNetRef;
let messageListenerAttached = false;
let serviceWorkerReg;

function isSupported() {
    return ('serviceWorker' in navigator) && ('indexedDB' in self);
}

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

function store(db, mode) {
    return db.transaction(SHINY_HTTP_STORE, mode).objectStore(SHINY_HTTP_STORE);
}

function promisify(req) {
    return new Promise((resolve, reject) => {
        req.onsuccess = () => resolve(req.result);
        req.onerror = () => reject(req.error);
    });
}

export async function init(swPath, ref) {
    if (!isSupported()) return false;
    dotNetRef = ref;

    if (swPath) {
        serviceWorkerReg = await navigator.serviceWorker.register(swPath);
        await navigator.serviceWorker.ready;
    } else {
        serviceWorkerReg = await navigator.serviceWorker.getRegistration();
    }

    if (!messageListenerAttached) {
        navigator.serviceWorker.addEventListener('message', handleMessage);
        messageListenerAttached = true;
    }
    return true;
}

function handleMessage(event) {
    if (!dotNetRef || !event.data || !event.data.shinyHttp) return;
    const m = event.data;
    switch (m.type) {
        case 'status':
            dotNetRef.invokeMethodAsync('OnStatusChanged', m.identifier, m.status);
            break;
        case 'completed':
            dotNetRef.invokeMethodAsync('OnTransferCompleted', m.identifier);
            break;
        case 'error':
            dotNetRef.invokeMethodAsync('OnTransferError', m.identifier, m.message || '', m.statusCode || 0);
            break;
    }
}

export async function queue(entry) {
    const db = await openDb();
    try {
        await promisify(store(db, 'readwrite').put(entry));
    } finally {
        db.close();
    }

    // Try Background Sync first (deferred-but-guaranteed when supported)
    try {
        if (serviceWorkerReg && 'sync' in serviceWorkerReg) {
            await serviceWorkerReg.sync.register(SHINY_HTTP_SYNC_TAG);
        }
    } catch (_) { /* not supported — fall back to immediate drain */ }

    // Always nudge the SW to attempt the drain right now while the page is open.
    if (serviceWorkerReg && serviceWorkerReg.active) {
        serviceWorkerReg.active.postMessage({ shinyHttp: true, type: 'drain' });
    }
}

export async function getAll() {
    const db = await openDb();
    try {
        const rows = await promisify(store(db, 'readonly').getAll());
        // Strip resultBlob from list responses to keep payloads small
        return rows.map(r => {
            const { resultBlob, body, ...rest } = r;
            return rest;
        });
    } finally {
        db.close();
    }
}

export async function get(identifier) {
    const db = await openDb();
    try {
        const row = await promisify(store(db, 'readonly').get(identifier));
        if (!row) return null;
        const { resultBlob, body, ...rest } = row;
        return rest;
    } finally {
        db.close();
    }
}

export async function remove(identifier) {
    const db = await openDb();
    try {
        await promisify(store(db, 'readwrite').delete(identifier));
    } finally {
        db.close();
    }
}

// Marks a transfer 'paused'. The service worker drain only runs 'pending'/'error' entries,
// so a paused entry is skipped without being removed. NOTE: an already in-flight fetch cannot
// be aborted, so pause only guarantees a not-yet-started (or retry-pending) transfer stays
// parked. Returns false if the transfer is missing or already completed.
export async function pause(identifier) {
    const db = await openDb();
    try {
        const row = await promisify(store(db, 'readonly').get(identifier));
        if (!row || row.status === 'completed' || row.status === 'paused') return false;
        row.status = 'paused';
        row.updatedAt = Date.now();
        await promisify(store(db, 'readwrite').put(row));
        return true;
    } finally {
        db.close();
    }
}

// Re-queues a paused transfer and nudges the service worker to drain it. Downloads are not
// resumable on Blazor (the SW fetch returns a whole Blob), so a resumed download restarts.
export async function resume(identifier) {
    const db = await openDb();
    try {
        const row = await promisify(store(db, 'readonly').get(identifier));
        if (!row || row.status !== 'paused') return false;
        row.status = 'pending';
        row.updatedAt = Date.now();
        await promisify(store(db, 'readwrite').put(row));
    } finally {
        db.close();
    }

    try {
        if (serviceWorkerReg && 'sync' in serviceWorkerReg) {
            await serviceWorkerReg.sync.register(SHINY_HTTP_SYNC_TAG);
        }
    } catch (_) { /* not supported — immediate drain below */ }

    if (serviceWorkerReg && serviceWorkerReg.active) {
        serviceWorkerReg.active.postMessage({ shinyHttp: true, type: 'drain' });
    }
    return true;
}

export async function clear() {
    const db = await openDb();
    try {
        await promisify(store(db, 'readwrite').clear());
    } finally {
        db.close();
    }
}

// Returns the downloaded blob (if any) for a completed transfer as a base64
// string. Larger payloads should be retrieved via streamResult instead.
export async function getResultBase64(identifier) {
    const db = await openDb();
    try {
        const row = await promisify(store(db, 'readonly').get(identifier));
        if (!row || !row.resultBlob) return null;
        const buf = await row.resultBlob.arrayBuffer();
        const bytes = new Uint8Array(buf);
        let binary = '';
        for (let i = 0; i < bytes.length; i++) binary += String.fromCharCode(bytes[i]);
        return btoa(binary);
    } finally {
        db.close();
    }
}

// Sets the body for an upload entry. base64Body is decoded into a Blob and
// persisted alongside the entry. Call AFTER queue().
export async function setBody(identifier, base64Body, contentType) {
    const db = await openDb();
    try {
        const row = await promisify(store(db, 'readonly').get(identifier));
        if (!row) return;
        const binary = atob(base64Body);
        const bytes = new Uint8Array(binary.length);
        for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);
        row.body = new Blob([bytes], { type: contentType || 'application/octet-stream' });
        await promisify(store(db, 'readwrite').put(row));
    } finally {
        db.close();
    }
}
