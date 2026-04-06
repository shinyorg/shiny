// Shiny Push Service Worker
// This file can be used as a standalone service worker, or its handlers can be
// imported into your own service worker via:
//   importScripts('./_content/Shiny.Push.Blazor/push-sw.js');

self.addEventListener('push', event => {
    let payload = {};
    try {
        payload = event.data ? event.data.json() : {};
    } catch (e) {
        payload = { body: event.data ? event.data.text() : '' };
    }

    const title = payload.title || (payload.notification && payload.notification.title) || 'Notification';
    const body = payload.body || (payload.notification && payload.notification.body) || '';
    const icon = payload.icon || (payload.notification && payload.notification.icon);
    const data = payload.data || {};

    event.waitUntil((async () => {
        await self.registration.showNotification(title, {
            body: body,
            icon: icon,
            data: { title: title, body: body, data: data }
        });
        await broadcastToClients({
            shinyPush: true,
            type: 'push-received',
            title: title,
            body: body,
            data: data
        });
    })());
});

self.addEventListener('notificationclick', event => {
    event.notification.close();
    const stored = event.notification.data || {};

    event.waitUntil((async () => {
        await broadcastToClients({
            shinyPush: true,
            type: 'notification-click',
            title: stored.title || null,
            body: stored.body || null,
            data: stored.data || {}
        });

        const allClients = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
        for (const client of allClients) {
            if ('focus' in client) {
                return client.focus();
            }
        }
        if (self.clients.openWindow) {
            return self.clients.openWindow('/');
        }
    })());
});

self.addEventListener('pushsubscriptionchange', event => {
    event.waitUntil((async () => {
        const oldOptions = event.oldSubscription ? event.oldSubscription.options : null;
        if (!oldOptions) return;

        const subscription = await self.registration.pushManager.subscribe(oldOptions);
        await broadcastToClients({
            shinyPush: true,
            type: 'subscription-change',
            subscription: JSON.stringify(subscription),
            endpoint: subscription.endpoint
        });
    })());
});

async function broadcastToClients(message) {
    const allClients = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
    for (const client of allClients) {
        client.postMessage(message);
    }
}
