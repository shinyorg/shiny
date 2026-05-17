var conn;
var dotNetRef;

export function init() {
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
