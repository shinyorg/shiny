using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;


namespace Shiny.SpeechRecognition
{
    public abstract class AbstractSpeechRecognizer : ISpeechRecognizer
    {
        protected Subject<bool> ListenSubject { get; } = new Subject<bool>();


        public IObservable<bool> WhenListeningStatusChanged() => this.ListenSubject;
        public abstract IObservable<string> ListenUntilPause();
        public abstract IObservable<string> ContinuousDictation();
        public abstract Task<AccessState> RequestAccess();
    }
}
