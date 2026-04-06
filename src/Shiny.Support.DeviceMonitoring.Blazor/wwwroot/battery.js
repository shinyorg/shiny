var battery;
var dotNetRef;

export async function init() {
    if (navigator.getBattery) {
        battery = await navigator.getBattery();
    }
}

export function isCharging() {
    if (!battery) return false;
    return battery.charging || false;
}

export function getLevel() {
    if (!battery) return 1.0;
    return battery.level;
}

export function startListener(objRef) {
    dotNetRef = objRef;
    if (!battery) return;

    battery.addEventListener('levelchange', onChange);
    battery.addEventListener('chargingchange', onChange);
}

export function stopListener() {
    if (battery) {
        battery.removeEventListener('levelchange', onChange);
        battery.removeEventListener('chargingchange', onChange);
    }
    dotNetRef = null;
}

function onChange() {
    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnChange');
    }
}
