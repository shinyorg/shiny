const sr = new SpeechRecognition();
var dotNetRef;

export function requestAccess() {
    if (window.SpeechRecognition === undefined)
        return 'notsupported';

    return 'granted';
}

export function startListener(lang, continuous, interim, dotNetRef) {
    sr.continuous = continuous;
    sr.interimResults = interim;
    sr.lang = lang;

    //sr.onaudiostart = e => {}
    //sr.onaudioend = e => {}
    //sr.onend = e => { }

    sr.onresult = e => {
        console.log('SPEECH RESULT', e);
        //dotNetRef
    };
    sr.start();
}

export function stopListener() {
    sr.stop();
    sr.onresult = null;
}