var reg;

export async function init() {
    reg = await navigator.serviceWorker.ready;
    //self.addEventListener('periodicsync', (event) => {
    //    if (event.tag === 'get-latest-news') {
    //        event.waitUntil(fetchAndCacheLatestNews());
    //    }
    //});
}

// TODO: request permission for background-fetch or background-sync
export async function register() {
    await reg.periodicSync.register('shiny', {
        minInterval: 24 * 60 * 60 * 1000
    });

}


export function isRegistered() {

}


export async function unregister() {
    await reg.per.unregister('shiny');
}