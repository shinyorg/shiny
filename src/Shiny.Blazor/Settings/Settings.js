window.AcrSettings = {
    contains: function(key) {
        return localStorage.getItem(key) !== null;
    },
    get: function (key) {
        return localStorage.getItem(key);
    },
    set: function(key, value) {
        localStorage.setItem(key, value);
    },
    remove: function(key) {
        localStorage.removeItem(key);
    },
    clear: function() {
        localStorage.clear();
    },
    list: function () {
        var arr = [];
        for (var i = 0; i < localStorage.length; i++) {
            var key = localStorage.key(i);
            var value = localStorage.getItem(key);
            arr.push({
                key: key,
                value: value
            });
        }
        return arr;
    }
};