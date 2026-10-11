let dotNetRef;
let serviceWorkerReg;
let messageListenerAttached = false;

function isSupported() {
    return ('Notification' in window)
        && ('serviceWorker' in navigator)
        && ('PushManager' in window);
}

export function getPermission() {
    if (!isSupported()) return 'unsupported';
    return Notification.permission;
}

export async function subscribe(vapidPublicKey, swPath, ref) {
    if (!isSupported()) return null;

    dotNetRef = ref;

    const permission = await Notification.requestPermission();
    if (permission !== 'granted') {
        return { permission: permission, subscription: null, endpoint: null };
    }

    serviceWorkerReg = await navigator.serviceWorker.register(swPath);
    await navigator.serviceWorker.ready;

    let subscription = await serviceWorkerReg.pushManager.getSubscription();
    if (!subscription) {
        subscription = await serviceWorkerReg.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: urlB64ToUint8Array(vapidPublicKey)
        });
    }

    if (!messageListenerAttached) {
        navigator.serviceWorker.addEventListener('message', handleServiceWorkerMessage);
        messageListenerAttached = true;
    }

    return {
        permission: 'granted',
        subscription: JSON.stringify(subscription),
        endpoint: subscription.endpoint
    };
}

export async function unsubscribe() {
    if (!serviceWorkerReg) {
        if (!isSupported()) return;
        serviceWorkerReg = await navigator.serviceWorker.getRegistration();
        if (!serviceWorkerReg) return;
    }

    const subscription = await serviceWorkerReg.pushManager.getSubscription();
    if (subscription) {
        await subscription.unsubscribe();
    }

    if (messageListenerAttached) {
        navigator.serviceWorker.removeEventListener('message', handleServiceWorkerMessage);
        messageListenerAttached = false;
    }
}

function handleServiceWorkerMessage(event) {
    if (!dotNetRef || !event.data || !event.data.shinyPush) return;

    const msg = event.data;
    switch (msg.type) {
        case 'push-received':
            dotNetRef.invokeMethodAsync('OnPushReceived', msg.data || {}, msg.title || null, msg.body || null);
            break;
        case 'notification-click':
            dotNetRef.invokeMethodAsync('OnNotificationClicked', msg.data || {}, msg.title || null, msg.body || null);
            break;
        case 'subscription-change':
            dotNetRef.invokeMethodAsync('OnSubscriptionChanged', msg.subscription, msg.endpoint);
            break;
    }
}

function urlB64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - base64String.length % 4) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const rawData = atob(base64);
    const outputArray = new Uint8Array(rawData.length);
    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}
