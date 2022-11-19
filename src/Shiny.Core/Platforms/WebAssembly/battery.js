var battery;
var batteryExports;

export async function init() {
    battery = await navigator.getBattery();
}

export function startListener() { 
    battery.addEventListener('levelchange', onChange);
    battery.addEventListener('chargingtimechange', onChange);
    battery.addEventListener('dischargingtimechange', onChange);

    //const { getAssemblyExports } = await globalThis.getDotnetRuntime(0);
    //var exports = await getAssemblyExports("BlazorSample.dll");

    //document.getElementById("result").innerText = exports.BlazorSample.JavaScriptInterop.Interop.GetMessageFromDotnet();
}

export function stopListener() {
    battery.removeEventListener('levelchange', onChange);
    battery.removeEventListener('chargingtimechange', onChange);
    battery.removeEventListener('dischargingtimechange', onChange);
    //dotNetRef = null;
}

export function isCharging() {
    return battery.charging || false;
}

export function getLevel() {
    //battery.chargingTime / dischargingTime 
    return battery.level || -1;
}