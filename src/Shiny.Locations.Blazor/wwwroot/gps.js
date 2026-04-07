let watchId = null;
let dotNetRef = null;

function isSupported() {
    return 'geolocation' in navigator;
}

export function requestAccess() {
    return new Promise((resolve) => {
        if (!isSupported()) {
            resolve('notsupported');
            return;
        }

        if (!('permissions' in navigator)) {
            // Older browsers - trigger a getCurrentPosition to force the prompt
            navigator.geolocation.getCurrentPosition(
                () => resolve('granted'),
                (err) => resolve(err.code === 1 ? 'denied' : 'prompt')
            );
            return;
        }

        navigator.permissions
            .query({ name: 'geolocation' })
            .then(result => {
                if (result.state !== 'prompt') {
                    resolve(result.state);
                    return;
                }

                // Force the browser to prompt the user, then read the new state
                navigator.geolocation.getCurrentPosition(
                    () => resolve('granted'),
                    (err) => resolve(err.code === 1 ? 'denied' : 'prompt')
                );
            })
            .catch(() => resolve('prompt'));
    });
}

export function getCurrent() {
    return new Promise((resolve, reject) => {
        if (!isSupported()) {
            resolve(null);
            return;
        }
        navigator.geolocation.getCurrentPosition(
            pos => resolve(toResult(pos)),
            err => reject(err.message || 'Geolocation error')
        );
    });
}

export function startListener(ref, highAccuracy) {
    if (!isSupported()) return;
    if (watchId !== null) return;

    dotNetRef = ref;
    watchId = navigator.geolocation.watchPosition(
        pos => {
            try {
                dotNetRef.invokeMethodAsync('OnReading', toResult(pos));
            } catch (e) {
                console.error('Shiny.Locations.Blazor OnReading failed', e);
            }
        },
        err => {
            try {
                dotNetRef.invokeMethodAsync('OnError', err.message || 'Geolocation error');
            } catch (e) {
                console.error('Shiny.Locations.Blazor OnError failed', e);
            }
        },
        {
            enableHighAccuracy: !!highAccuracy,
            maximumAge: 0
        }
    );
}

export function stopListener() {
    if (watchId !== null) {
        navigator.geolocation.clearWatch(watchId);
        watchId = null;
    }
    dotNetRef = null;
}

function toResult(pos) {
    return {
        timestamp: pos.timestamp,
        accuracy: pos.coords.accuracy,
        altitude: pos.coords.altitude,
        altitudeAccuracy: pos.coords.altitudeAccuracy,
        heading: pos.coords.heading,
        latitude: pos.coords.latitude,
        longitude: pos.coords.longitude,
        speed: pos.coords.speed
    };
}
