var watchId;

export function requestAccess() {
    return new Promise((resolve, reject) => {
        if (navigator.geolocation === undefined) {
            resolve('notsupported');
        }
        else {
            navigator
                .permissions
                .query({ name: 'geolocation' })
                .then(result => {
                    if (result.state != 'prompt') {
                        resolve(result.state); 
                    }
                    else {
                        result.onchange = () => {
                            resolve(result.state);
                        };
                        // throw away

                        navigator.geolocation.getCurrentPosition(_ => { }); 
                    }
                });
        }
    });
}

export function getCurrent() {
    return new Promise((resolve, reject) => {
        navigator
            .geolocation
            .getCurrentPosition(
                function (pos) {
                    var r = toResult(pos);
                    resolve(r);
                },
                function (e) {
                    reject(e);
                }
                //{
                //    maximumAge: integer(milliseconds) | infinity - maximum cached position age.
                //    timeout: integer(milliseconds) - amount of time before the error callback is invoked, if 0 it will never invoke.
                //    enableHighAccuracy: false | true
                //}
            )
    });
}

export function startListener(dotNetRef) {
    watchId = navigator
        .geolocation
        .watchPosition(
            function (pos) {
                var r = toResult(pos);
                dotNetRef.invokeMethod("Success", r);
            },
            function (e) {
                dotNetRef.invokeMethod("Error", e);
            }
            //{
            //    maximumAge: integer(milliseconds) | infinity - maximum cached position age.
            //    timeout: integer(milliseconds) - amount of time before the error callback is invoked, if 0 it will never invoke.
            //    enableHighAccuracy: false | true
            //}
        );
}

export function stopListener() {
    navigator.geolocation.clearWatch(watchId);
}

function toResult(pos) {
    var e = {
        timestamp: pos.timestamp,
        accuracy: pos.coords.accuracy,
        altitude: pos.coords.altitude,
        altitudeAccuracy: pos.coords.altitudeAccuracy,
        heading: pos.coords.heading,
        latitude: pos.coords.latitude,
        longitude: pos.coords.longitude,
        speed: pos.coords.speed
    };
    //console.log('GEOLOCATION', e);
    return e;
}