// Shiny.Gamepad - the browser side of the W3C Gamepad API backend.
//
// The Gamepad API has no events for input: navigator.getGamepads() returns a fresh snapshot each
// call and the page is expected to read it once per frame. Doing that from .NET would mean a JS
// interop round trip per frame per controller, so the loop lives here instead and only calls into
// .NET when something actually changed. A player holding a controller perfectly still costs zero
// interop calls.
//
// The button bit positions below MUST match Shiny.Gamepad.GamepadButton. They are packed here
// rather than in .NET so one number crosses the boundary instead of an array of booleans.

const Button = {
    A: 1 << 0,
    B: 1 << 1,
    X: 1 << 2,
    Y: 1 << 3,
    LeftShoulder: 1 << 4,
    RightShoulder: 1 << 5,
    LeftTrigger: 1 << 6,
    RightTrigger: 1 << 7,
    LeftStick: 1 << 8,
    RightStick: 1 << 9,
    DPadUp: 1 << 10,
    DPadDown: 1 << 11,
    DPadLeft: 1 << 12,
    DPadRight: 1 << 13,
    Start: 1 << 14,
    Select: 1 << 15,
    Home: 1 << 16,
    Touchpad: 1 << 17
};

// the standard mapping's button order, as the specification fixes it
const STANDARD_BUTTONS = [
    Button.A, Button.B, Button.X, Button.Y,
    Button.LeftShoulder, Button.RightShoulder,
    Button.LeftTrigger, Button.RightTrigger,
    Button.Select, Button.Start,
    Button.LeftStick, Button.RightStick,
    Button.DPadUp, Button.DPadDown, Button.DPadLeft, Button.DPadRight,
    Button.Home,
    Button.Touchpad
];

const LEFT_TRIGGER_INDEX = 6;
const RIGHT_TRIGGER_INDEX = 7;

// Chrome caps a single dual-rumble effect at five seconds, and the API has no "run until stopped"
// form at all. SetVibration promises a level that holds, so a level above zero is re-armed on a
// timer slightly shorter than the effect, which reads as continuous.
const EFFECT_DURATION_MS = 5000;
const REARM_INTERVAL_MS = 4000;

let dotNet = null;
let frameHandle = null;
let running = false;
const lastStates = new Map();
const rumble = new Map();
let rearmHandle = null;


function describe(pad) {
    return {
        index: pad.index,
        id: pad.id ?? 'Gamepad',
        mapping: pad.mapping ?? '',
        connected: pad.connected === true,
        canVibrate: !!pad.vibrationActuator
    };
}


function snapshot(pad) {
    let buttons = 0;

    const count = Math.min(pad.buttons.length, STANDARD_BUTTONS.length);
    for (let i = 0; i < count; i++) {
        if (pad.buttons[i] && pad.buttons[i].pressed) {
            buttons |= STANDARD_BUTTONS[i];
        }
    }

    // the triggers are analog buttons rather than axes in this API - value, not pressed, is what a
    // game needs, and pressed is already folded into the mask above
    const leftTrigger = pad.buttons[LEFT_TRIGGER_INDEX] ? pad.buttons[LEFT_TRIGGER_INDEX].value : 0;
    const rightTrigger = pad.buttons[RIGHT_TRIGGER_INDEX] ? pad.buttons[RIGHT_TRIGGER_INDEX].value : 0;

    const axes = pad.axes ?? [];

    return {
        index: pad.index,
        buttons: buttons,
        leftX: axes[0] ?? 0,
        // the browser reports stick Y positive downwards; every backend in this library reports it
        // positive upwards, so it is flipped here rather than in six different places
        leftY: -(axes[1] ?? 0),
        rightX: axes[2] ?? 0,
        rightY: -(axes[3] ?? 0),
        leftTrigger: leftTrigger,
        rightTrigger: rightTrigger
    };
}


function changed(previous, next) {
    if (!previous) {
        return true;
    }

    if (previous.buttons !== next.buttons) {
        return true;
    }

    // a bare inequality would report a change every frame from stick noise; the threshold here is
    // deliberately far below IGamepadManager.AxisChangeThreshold, which does the real filtering in
    // .NET - this only decides whether the interop call is worth making at all
    const epsilon = 0.0005;

    return Math.abs(previous.leftX - next.leftX) > epsilon
        || Math.abs(previous.leftY - next.leftY) > epsilon
        || Math.abs(previous.rightX - next.rightX) > epsilon
        || Math.abs(previous.rightY - next.rightY) > epsilon
        || Math.abs(previous.leftTrigger - next.leftTrigger) > epsilon
        || Math.abs(previous.rightTrigger - next.rightTrigger) > epsilon;
}


function pump() {
    if (!running) {
        return;
    }

    const pads = navigator.getGamepads ? navigator.getGamepads() : [];
    const updates = [];

    for (const pad of pads) {
        if (!pad || !pad.connected) {
            continue;
        }

        const next = snapshot(pad);
        if (changed(lastStates.get(pad.index), next)) {
            lastStates.set(pad.index, next);
            updates.push(next);
        }
    }

    if (updates.length > 0 && dotNet) {
        dotNet.invokeMethodAsync('OnStates', updates);
    }

    frameHandle = requestAnimationFrame(pump);
}


function applyRumble(index, low, high) {
    const pad = (navigator.getGamepads ? navigator.getGamepads() : [])[index];
    if (!pad || !pad.vibrationActuator) {
        return false;
    }

    if (low <= 0 && high <= 0) {
        if (pad.vibrationActuator.reset) {
            pad.vibrationActuator.reset();
        } else {
            pad.vibrationActuator.playEffect('dual-rumble', {
                duration: 0,
                strongMagnitude: 0,
                weakMagnitude: 0
            });
        }
        return true;
    }

    pad.vibrationActuator.playEffect('dual-rumble', {
        startDelay: 0,
        duration: EFFECT_DURATION_MS,
        strongMagnitude: low,
        weakMagnitude: high
    }).catch(() => { /* a browser that advertises the actuator but refuses the effect */ });

    return true;
}


function rearm() {
    for (const [index, level] of rumble) {
        if (level.low > 0 || level.high > 0) {
            applyRumble(index, level.low, level.high);
        }
    }
}


export function probe() {
    return {
        supported: typeof navigator.getGamepads === 'function',
        secureContext: window.isSecureContext === true
    };
}


export function start(reference) {
    dotNet = reference;

    if (running) {
        return list();
    }

    running = true;

    window.addEventListener('gamepadconnected', onConnected);
    window.addEventListener('gamepaddisconnected', onDisconnected);

    rearmHandle = setInterval(rearm, REARM_INTERVAL_MS);
    frameHandle = requestAnimationFrame(pump);

    return list();
}


function onConnected(e) {
    if (dotNet) {
        dotNet.invokeMethodAsync('OnConnected', describe(e.gamepad));
    }
}


function onDisconnected(e) {
    lastStates.delete(e.gamepad.index);
    rumble.delete(e.gamepad.index);

    if (dotNet) {
        dotNet.invokeMethodAsync('OnDisconnected', e.gamepad.index);
    }
}


export function list() {
    const pads = navigator.getGamepads ? navigator.getGamepads() : [];
    const result = [];

    for (const pad of pads) {
        if (pad && pad.connected) {
            result.push(describe(pad));
        }
    }

    return result;
}


export function setVibration(index, low, high) {
    rumble.set(index, { low: low, high: high });

    return applyRumble(index, low, high);
}


export function stop() {
    running = false;

    if (frameHandle !== null) {
        cancelAnimationFrame(frameHandle);
        frameHandle = null;
    }

    if (rearmHandle !== null) {
        clearInterval(rearmHandle);
        rearmHandle = null;
    }

    for (const index of rumble.keys()) {
        applyRumble(index, 0, 0);
    }

    window.removeEventListener('gamepadconnected', onConnected);
    window.removeEventListener('gamepaddisconnected', onDisconnected);

    lastStates.clear();
    rumble.clear();
    dotNet = null;
}
