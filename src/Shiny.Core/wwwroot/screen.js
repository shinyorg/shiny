var dotNetRef;

function orientationType() {
    var type = screen.orientation && screen.orientation.type;
    if (!type) return null;
    return type.indexOf('portrait') === 0 ? 'portrait' : 'landscape';
}

export function getScreen() {
    return {
        width: window.screen.width,
        height: window.screen.height,
        density: window.devicePixelRatio || 1,
        orientation: orientationType()
    };
}

export function getViewportWidth() {
    return window.innerWidth;
}

export function getViewportHeight() {
    return window.innerHeight;
}

// Browsers only lock orientation for a full-screen page or an installed app - anything else rejects.
export async function setOrientation(value) {
    if (!screen.orientation) return false;
    try {
        if (value) {
            await screen.orientation.lock(value);
        } else {
            screen.orientation.unlock();
        }
        return true;
    } catch (e) {
        return false;
    }
}

function changed() {
    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnChange');
    }
}

export function startListener(objRef) {
    dotNetRef = objRef;
    // resize covers rotation, zoom (devicePixelRatio) and moving the window to another display
    window.addEventListener('resize', changed);
    if (screen.orientation) {
        screen.orientation.addEventListener('change', changed);
    }
}

export function stopListener() {
    window.removeEventListener('resize', changed);
    if (screen.orientation) {
        screen.orientation.removeEventListener('change', changed);
    }
    dotNetRef = null;
}
