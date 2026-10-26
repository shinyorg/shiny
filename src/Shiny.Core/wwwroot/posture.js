var dotNetRef;
var queries = [];

function segments() {
    var list = window.viewport && window.viewport.segments;
    if (!list || list.length < 2) return [];

    // flattened [x, y, width, height, ...] - a flat number array needs no trimming-sensitive DTO on the .NET side
    var values = [];
    for (var i = 0; i < list.length; i++) {
        var s = list[i];
        values.push(s.x, s.y, s.width, s.height);
    }
    return values;
}

export function getPosture() {
    var posture = navigator.devicePosture;
    var hasSegments = !!(window.viewport && 'segments' in window.viewport);
    return {
        supported: !!posture || hasSegments,
        type: posture ? posture.type : null,
        segments: segments()
    };
}

function changed() {
    if (dotNetRef) {
        dotNetRef.invokeMethodAsync('OnChange');
    }
}

export function startListener(objRef) {
    dotNetRef = objRef;
    if (navigator.devicePosture) {
        navigator.devicePosture.addEventListener('change', changed);
    }
    // segments change with the viewport, and the viewport segments API has no event of its own - a fold that
    // appears without the viewport changing size raises no resize, so the segment media queries are watched too
    window.addEventListener('resize', changed);
    if (window.matchMedia) {
        queries = ['(horizontal-viewport-segments: 2)', '(vertical-viewport-segments: 2)'].map(function (q) {
            var mq = window.matchMedia(q);
            mq.addEventListener('change', changed);
            return mq;
        });
    }
}

export function stopListener() {
    if (navigator.devicePosture) {
        navigator.devicePosture.removeEventListener('change', changed);
    }
    window.removeEventListener('resize', changed);
    queries.forEach(function (mq) { mq.removeEventListener('change', changed); });
    queries = [];
    dotNetRef = null;
}
