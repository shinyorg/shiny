
export function exists(key) {
    //return localStorage.getItem(key) == null;
    return localStorage.hasOwnProperty(key);
}

export function get(key) {
    return localStorage.getItem(key);
}

export function set(key, json) {
    const result = exists(key);
    localStorage.setItem(key, json);
    return result;
}

export function clear(key) {
    localStorage.clear();
}

export function remove(key) {
    const result = exists(key);
    localStorage.removeItem(key);
    return result;
}

export function getList() {
    var values = [];
    var keys = Object.keys(localStorage);
    var i = keys.length;

    while (i--)
    {
        var value = localStorage.getItem(keys[i]);
        values.push({ key: value });
    }
    return values;
}