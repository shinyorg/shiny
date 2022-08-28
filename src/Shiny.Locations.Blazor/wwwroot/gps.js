var watchId;

export function requestAccess() {
    return new Promise((resolve, reject) => {
        if (navigator.geolocation == undefined) {
            resolve('notsupported');
        }
        else {
            navigator
                .permissions
                .query({ name: 'geolocation' })
                .then(result => {
                    resolve(result.state);
                });
        }
    });
}

//    whenStatusChanged: function (dotNetRef) {
//        navigator
//            .permissions
//            .query({ name: 'geolocation' })
//            .then(function (result) {
//                result.onchange = function () {
//                    dotNetRef.invokeMethod('Success', result.state);
//                }
//            });
//    },

export function getCurrent() {
    return new Promise((resolve, reject) => {
        navigator
            .geolocation
            .getCurrentPosition(
                function (pos) {
                    resolve(pos);
                },
                function (e) {
                    reject(e);
                }
                //                    //{
                //                    //    maximumAge: integer(milliseconds) | infinity - maximum cached position age.
                //                    //    timeout: integer(milliseconds) - amount of time before the error callback is invoked, if 0 it will never invoke.
                //                    //    enableHighAccuracy: false | true
                //                    //}
            )
    });
}

export function startListener(dotNetRef) {
    this.watchId = navigator
        .geolocation
        .watchPosition(
            function (pos) {
                //        const e = {
                //            timestamp: pos.timestamp,
                //            accuracy: pos.coords.accuracy,
                //            altitude: pos.coords.altitude,
                //            altitudeAccuracy: pos.coords.altitudeAccuracy,
                //            heading: pos.coords.heading,
                //            latitude: pos.coords.latitude,
                //            longitude: pos.coords.longitude,
                //            speed: pos.coords.speed
                //        };
                dotNetRef.invokeMethod("Success", e);
            },
            function (e) {
                console.log('ERROR', e);
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
    navigator.geolocation.clearWatch(this.watchId);
}