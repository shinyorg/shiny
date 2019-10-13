using System;


namespace Shiny.SpeechRecognition
{
    public static class CrossSpeechRecognizer
    {
        public static ISpeechRecognizer Current => ShinyHost.Resolve<ISpeechRecognizer>();
    }
}
