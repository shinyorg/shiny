var battery;
var dotNetRef;
var initialized = false;

// Must run before anything below reports a real value - navigator.getBattery() is a promise,
// so it cannot be resolved from the synchronous getters the IBattery contract exposes.
export async function init() {
    if (initialized) return;
    initialized = true;

    if (navigator.getBattery) {
        try {
            battery = await navigator.getBattery();
        }
        catch {
            // Some browsers expose the function but reject the call (insecure context,
            // permissions policy). Treat that as "no battery API".
            battery = undefined;
        }
    }
}

export function isSupported() {
    return !!battery;
}

export function isCharging() {
    if (!battery) return false;
    return battery.charging || false;
}

export function getLevel() {
    if (!battery) return 1.0;
    return battery.level;
}

export async function startListener(objRef) {
    await init();
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
