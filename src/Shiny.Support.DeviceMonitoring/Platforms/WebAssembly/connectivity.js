var conn;
var dotNetRef;

export function init() {
    conn = navigator.connection || navigator.mozConnection || navigator.webkitConnection;
}

export function getConnType() {
    return conn.type;
    // conn.type is not available in MSFT EDGE yet
}

export function isConnected() {
    return navigator.onLine;
}

export function getEffectiveType() {
    return conn.effectiveType;
}

export function startListener(objRef) {
    dotNetRef = objRef;
    window.addEventListener("online", updateConnectionStatus);
    window.addEventListener("offline", updateConnectionStatus);
    conn.addEventListener('change', updateConnectionStatus);
}

export function stopListener() {
    window.removeEventListener("online", updateConnectionStatus);
    window.removeEventListener("offline", updateConnectionStatus);
    conn.removeEventListener('change', updateConnectionStatus);
    dotNetRef = null;
}

function updateConnectionStatus(args) {
    dotNetRef.invokeMethod('OnChange');
}