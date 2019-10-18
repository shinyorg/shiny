using System;
using Shiny.SpeechRecognition;


namespace Shiny
{
    public static class CrossSpeechRecognizer
    {
        public static ISpeechRecognizer Current => ShinyHost.Resolve<ISpeechRecognizer>();
    }
}
