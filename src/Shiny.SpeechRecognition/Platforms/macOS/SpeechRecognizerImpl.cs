//using System;
//using System.Threading.Tasks;
//using AVFoundation;
//using Speech;
//using UIKit;

//https://developer.apple.com/documentation/appkit/nsspeechrecognizer
using System;
using System.Threading.Tasks;
using System.Reactive.Linq;
using AppKit;
using System.Reactive.Subjects;

namespace Shiny.SpeechRecognition
{
    public class SpeechRecognizerImpl : NSSpeechRecognizerDelegate, ISpeechRecognizer
    {
        readonly Subject<string> speechSubj = new Subject<string>();


        public override void DidRecognizeCommand(NSSpeechRecognizer sender, string command)
        {       
        }


        public IObservable<string> ContinuousDictation() => Observable.Create<string>(ob =>
        {
            var speech = new NSSpeechRecognizer();
            speech.Commands = new[] { "", "" };
            speech.Delegate = this;
            speech.StartListening();

            var sub = this.speechSubj.Subscribe(
                ob.OnNext,
                ob.OnError,
                ob.OnCompleted
            );

            return () =>
            {
                speech.StopListening();
                sub.Dispose();
            };
        });


        public IObservable<string> ListenUntilPause()
        {
            throw new NotImplementedException();
        }


        public Task<AccessState> RequestAccess()
        {
            
            throw new NotImplementedException();
        }


        public IObservable<bool> WhenListeningStatusChanged()
        {
            throw new NotImplementedException();
        }
    }
}