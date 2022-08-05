//https://www.twilio.com/blog/speech-recognition-browser-web-speech-api

const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition
if (typeof SpeechRecognition === "undefined") {
} else {
    // good stuff to come here
}

speechRecognition = new webkitSpeechRecognition();
speechRecognition.onresult = console.log;
speechRecognition.start();

recognition.continuous = true;
recognition.interimResults = true;
recognition.addEventListener("result", onResult);