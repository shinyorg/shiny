using System;
using System.Globalization;
using System.Reactive.Subjects;
using System.Threading.Tasks;


namespace Shiny.SpeechRecognition
{
    public abstract class AbstractSpeechRecognizer : ISpeechRecognizer
    {
        protected Subject<bool> ListenSubject { get; } = new Subject<bool>();


        public IObservable<bool> WhenListeningStatusChanged() => this.ListenSubject;
        public abstract IObservable<string> ListenUntilPause(CultureInfo? culture = null);
        public abstract IObservable<string> ContinuousDictation(CultureInfo? culture = null);
        public abstract Task<AccessState> RequestAccess();
    }
}
