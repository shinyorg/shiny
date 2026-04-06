// Shiny storage helpers for Blazor WebAssembly.
// Attached to window so IJSInProcessRuntime can invoke them synchronously.
window.shinyLocalStorage = {
    getItem: function (key) {
        return localStorage.getItem(key);
    },
    setItem: function (key, value) {
        localStorage.setItem(key, value);
    },
    removeItem: function (key) {
        var existed = localStorage.getItem(key) !== null;
        localStorage.removeItem(key);
        return existed;
    },
    containsKey: function (key) {
        return localStorage.getItem(key) !== null;
    },
    getKeys: function (prefix) {
        var out = [];
        for (var i = 0; i < localStorage.length; i++) {
            var k = localStorage.key(i);
            if (k && k.indexOf(prefix) === 0)
                out.push(k);
        }
        return out;
    },
    removeKeys: function (prefix) {
        var toRemove = [];
        for (var i = 0; i < localStorage.length; i++) {
            var k = localStorage.key(i);
            if (k && k.indexOf(prefix) === 0)
                toRemove.push(k);
        }
        for (var j = 0; j < toRemove.length; j++)
            localStorage.removeItem(toRemove[j]);
        return toRemove.length;
    }
};
