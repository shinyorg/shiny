var battery;
var dotNetRef;

function onChange() {
    dotNetRef.invokeMethod('OnChange');
}

export async function init() {
    battery = await navigator.getBattery();
}

export function startListener(dotNetRefObj) {
    dotNetRef = dotNetRefObj;
    battery.addEventListener('levelchange', onChange);
    battery.addEventListener('chargingtimechange', onChange);
    battery.addEventListener('dischargingtimechange', onChange);
}

export function stopListener() {
    battery.removeEventListener('levelchange', onChange);
    battery.removeEventListener('chargingtimechange', onChange);
    battery.removeEventListener('dischargingtimechange', onChange);
    dotNetRef = null;
}

export function isCharging() {
    return battery.charging;
}

export function getLevel() {
    //battery.chargingTime / dischargingTime 
    return battery.level;
}