var shinyGps = {
    watchId: 0,

    requestAccess: function () {
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
    },

    whenStatusChanged: function (dotNetRef) {
        navigator
            .permissions
            .query({ name: 'geolocation' })
            .then(function (result) {
                result.onchange = function () {
                    dotNetRef.invokeMethod('Success', result.state);
                }
            });
    },

    getCurrentGps: function () {
        let me = this;
        return new Promise((resolve, reject) => {
            navigator
                .geolocation
                .getCurrentPosition(
                    function (pos) {
                        const e = me.process(pos);
                        resolve(e);
                    },
                    function (e) {
                        reject(e);
                    }
                    //{
                    //    maximumAge: integer(milliseconds) | infinity - maximum cached position age.
                    //    timeout: integer(milliseconds) - amount of time before the error callback is invoked, if 0 it will never invoke.
                    //    enableHighAccuracy: false | true
                    //}
                );
        });
    },

    startListener: function (dotNetRef) {
        let me = this;
        this.watchId = navigator
            .geolocation
            .watchPosition(
                function (pos) {
                    const e = me.process(pos);
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
    },

    stopListener: function() {
        navigator.geolocation.clearWatch(this.watchId);
    },

    process: function (pos) {
        const e = {
            timestamp: pos.timestamp,
            accuracy: pos.coords.accuracy,
            altitude: pos.coords.altitude,
            altitudeAccuracy: pos.coords.altitudeAccuracy,
            heading: pos.coords.heading,
            latitude: pos.coords.latitude,
            longitude: pos.coords.longitude,
            speed: pos.coords.speed
        };
        console.log('POS: ', e);
        return e;
    }
}

export { shinyGps };