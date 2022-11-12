//https://developer.mozilla.org/en-US/docs/Web/API/Notifications_API/Using_the_Notifications_API

export function requestAccess() {
    //Notification.permission === 'granted' ? 'none' : 'block';
    Notification.requestPermission().then((result) => {
        console.log(result);
    });
}


export function send() {
    const img = '/to-do-notifications/img/icon-128.png';
    const text = `HEY! Your task "${title}" is now overdue.`;
    const notification = new Notification('To do list', { body: text, icon: img });

    //const action = new NotificationAction();

    //notification.actions.concat()
    //notification.close();

    //notification.image
    //notification.data
    //notification.silent
    //notification.actions
    //notification.vibrate
    //notification.tag
    //notification.timestamp

    notification.onclick = e => {

    };
    notification.onclose = e => {

    };
    notification.onerror = e => {

    };
    notification.onshow = e => {

    };
    
    //const n = new Notification('My Great Song');
    //document.addEventListener('visibilitychange', () => {
    //    if (document.visibilityState === 'visible') {
    //        // The tab has become visible so clear the now-stale Notification.
    //        n.close();
    //    }
    //});
}