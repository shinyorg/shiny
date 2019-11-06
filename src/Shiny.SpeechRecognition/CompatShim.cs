using System;
using System.Threading.Tasks;
using Shiny.SpeechRecognition;


namespace Shiny
{
    public static class CrossSpeechRecognizer
    {
        static ISpeechRecognizer Current => ShinyHost.Resolve<ISpeechRecognizer>();

        public static IObservable<string> ContinuousDictation() => Current.ContinuousDictation();
        public static IObservable<string> ListenUntilPause() => Current.ListenUntilPause();
        public static Task<AccessState> RequestAccess() => Current.RequestAccess();
        public static IObservable<bool> WhenListeningStatusChanged() => Current.WhenListeningStatusChanged();
    }
}
