let handler;

window.MediatorServices = {
    isOnline: function() {
        return navigator.onLine;    
    },
    
    subscribe: function(interop) {

        handler = function() {
            interop.invokeMethodAsync("MediatorServices.OnStatusChanged", navigator.onLine);
        }

        window.addEventListener("online", handler);
        window.addEventListener("offline", handler);
    },
    
    unsubscribe: function() {
        if (handler == null) 
            return;

        window.removeEventListener("online", handler);
        window.removeEventListener("offline", handler);
    },
    
    setStore: function(key, value) {
        localStorage.setItem(key, value);
    },
    
    getStore: function(key) {
        return localStorage.getItem(key);
    },
    
    removeStore: function(key) {
        localStorage.removeItem(key);
    },
    
    removeByPrefix: function(key) {
        Object.keys(localStorage).forEach(key => {
            if (key.startsWith(key)) 
                delete localStorage[key];
        })
    },
    
    clearStore: function() {
        localStorage.clear();
    }
};