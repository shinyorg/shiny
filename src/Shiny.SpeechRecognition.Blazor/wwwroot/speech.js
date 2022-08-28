const sr = new SpeechRecognition();
var dotNetRef;


export function startListener(dotNetRef) {
    //sr.continuous = true;
    //sr.interimResults = true;
    sr.onaudiostart = e => {

    };

    sr.onaudioend = e => {

    };

    sr.onresult = e => {

    };

    sr.onend = e => {

    };
    sr.start();
}

export function stopListener() {
    sr.stop();
}
//const speechRecognitionList = new SpeechGrammarList();
//speechRecognitionList.addFromString(grammar, 1);
//recognition.grammars = speechRecognitionList;
////recognition.continuous = false;
//recognition.lang = 'en-US';
//recognition.interimResults = false;
//recognition.maxAlternatives = 1;