namespace Shiny {
    class Permissions {

        public requestAccess(queryArgs: any): any {
            return navigator
                .permissions
                .query(queryArgs);
        }

        public whenStatusChanged(dotNetRef: any, queryArgs: any) {
            navigator
                .permissions
                .query(queryArgs)
                .then(result => {
                    result.onchange = function () {
                        dotNetRef.invokeMethod('Success', result.state);
                    }
                });
        }
    }

    export function loadPermissions(): void {
        window['shinypermissions'] = new Permissions();
    }
}
Shiny.loadPermissions();

//navigator.permissions.query({
    //    name: "bluetooth",
    //    deviceId: sessionStorage.lastDevice,
    //}).then(result => {
    //    if (result.devices.length == 1) {
    //        return result.devices[0];
    //    } else {
    //        throw new DOMException("Lost permission", "NotFoundError");
    //    }
    //}).then(...);