var conn;
var dotNetRef;
var initialized = false;

// Resolves navigator.connection once. Without it getConnType() only ever answers 'unknown'.
export function init() {
    if (initialized) return;
    initialized = true;

    conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
}

export function getConnType() {
    if (!conn || !conn.type) return 'unknown';
    return conn.type;
}

export function isConnected() {
    return navigator.onLine;
}

export function getEffectiveType() {
    if (!conn || !conn.effectiveType) return 'unknown';
    return conn.effectiveType;
}

export function startListener(objRef) {
    init();
    dotNetRef = objRef;
    window.addEventListener('online', updateConnectionStatus);
    window.addEventListener('offline', updateConnectionStatus);
    if (conn) {
        conn.addEventListener('change', updateConnectionStatus);
    }
}

export function stopListener() {
    window.removeEventListener('online', updateConnectionStatus);
    window.removeEventListener('offline', updateConnectionStatus);
    if (conn) {
        conn.removeEventListener('change', updateConnectionStatus);
    }
    dotNetRef = null;
}

function updateConnectionStatus() {
    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnChange');
    }
}
