//https://developer.mozilla.org/en-US/docs/Web/API/Push_API
var reg;
var dotNetRef;

export async function init(objRef) {
    reg = await navigator.serviceWorker.ready;
    dotNetRef = objRef;

    reg.pushManager.onpushsubscriptionchange = e => {
        // TODO: ontokenchanged

        //const subscription = swRegistration.pushManager
        //    .subscribe(event.oldSubscription.options)
        //    .then((subscription) =>
        //        fetch("register", {
        //            method: "post",
        //            headers: {
        //                "Content-type": "application/json",
        //            },
        //            body: JSON.stringify({
        //                endpoint: subscription.endpoint,
        //            }),
        //        }),
        //    );
        //event.waitUntil(subscription);
    };

    reg.pushManager.onpush = e => {
        //let message = e.data.json();

        //switch (message.type) {
        //    case "init":
        //        doInit();
        //        break;
        //    case "shutdown":
        //        doShutdown();
        //        break;
        //}
    };

    self.addEventListener('push', e => {
        //if (!(self.Notification && self.Notification.permission === 'granted')) {
        //    return;
        //}

        //const data = event.data?.json() ?? {};
        //const title = data.title || "Something Has Happened";
        //const message = data.message || "Here's something you might want to check out.";
        //const icon = "images/new-notification.png";

        //const notification = new self.Notification(title, {
        //    body: message,
        //    tag: 'simple-push-demo-notification',
        //    icon,
        //});

        //notification.addEventListener('click', () => {
        //    clients.openWindow('https://example.blog.com/2015/03/04/something-new.html');
        //});
    });
}

export async function register(appServerKey) {

    try {
        var sub = await reg.pushManager.subscribe({
            userVisibleOnly: false,
            applicationServerKey: appServerKey
        });
        //sub.toJSON();
        //const endpoint = subscription.endpoint;
        //const key = subscription.getKey('p256dh');
        //const auth = subscription.getKey('auth');
        return sub.endpoint;
    }
    catch (e) {

    }
}

export async function unregister() {
    const sub = await reg.pushManager.getSubscription();
    if (sub != null) {
        await sub.unsubscribe();
    }
}