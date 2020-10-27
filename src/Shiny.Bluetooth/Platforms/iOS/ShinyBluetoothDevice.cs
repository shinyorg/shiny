using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using ExternalAccessory;

using Foundation;

namespace Shiny.Bluetooth
{
    public class ShinyBluetoothDevice : NSStreamDelegate, IBluetoothDevice
    {
        readonly EAAccessory accessory;
        readonly Subject<Unit> dataAvailSubj;
        EASession? session;


        public ShinyBluetoothDevice(EAAccessory accessory)
        {
            this.accessory = accessory;
            this.dataAvailSubj = new Subject<Unit>();
        }


        public string Name => this.accessory.Name;
        public ConnectionState Status => this.session == null ? ConnectionState.Disconnected : ConnectionState.Connected;


        public IObservable<Unit> Connect() => Observable.Create<Unit>(ob =>
        {
            this.session ??= new EASession(this.accessory, "PROTOCOL");
            this.HookStreams(this.session.InputStream, this.session.OutputStream);

            return Disposable.Empty;
        });


        public IObservable<Unit> Disconnect() => Observable.Create<Unit>(ob =>
        {
            this.KillStreams(this.session.InputStream, this.session.OutputStream);
            this.session.Dispose();
            this.session = null;

            return Disposable.Empty;
        });


        public IObservable<Unit> Write(byte[] data)
        {
            this.session.OutputStream.Write()
            throw new NotImplementedException();
        }


        public IObservable<ConnectionState> WhenStatusChanged()
        {
            throw new NotImplementedException();
        }


        public IObservable<Unit> WhenDataAvailable() => this.dataAvailSubj;


        public IObservable<uint> Read(byte[] buffer, uint length = 1024) => Observable.Create<uint>(ob =>
        {
            if (!this.session.InputStream.HasBytesAvailable())
            {
                ob.Respond((uint)0);
            }
            else
            {
                var read = this.session.InputStream.Read(buffer, length);
                ob.Respond((uint)read);
            }
            return Disposable.Empty;
        });


        public override void HandleEvent(NSStream theStream, NSStreamEvent streamEvent)
        {
            switch (streamEvent)
            {
                case NSStreamEvent.HasBytesAvailable:
                    this.dataAvailSubj.OnNext(Unit.Default);
                    break;

                case NSStreamEvent.HasSpaceAvailable:
                    // TODO: can write or write finished?
                    break;

                case NSStreamEvent.OpenCompleted:
                    break;

                case NSStreamEvent.ErrorOccurred:
                    break;

                case NSStreamEvent.EndEncountered:
                    break;

                case NSStreamEvent.None:
                    break;
            }
        }


        protected void HookStreams(params NSStream[] streams)
        {
            foreach (var stream in streams)
            {
                stream.Delegate = this;
                stream.Schedule(NSRunLoop.Current, NSRunLoopMode.Default);
                stream.Open();
            }
        }


        protected void KillStreams(params NSStream[] streams)
        {
            foreach (var stream in streams)
            {
                if (stream != null)
                {
                    stream.Delegate = null;
                    stream.Unschedule(NSRunLoop.Current, NSRunLoopMode.Default);
                    stream.Close();
                }
            }
        }
    }
}
//        switch (streamEvent)
//        {

//            case NSStreamEvent.None:
//                Console.WriteLine("StreamEventNone");
//                break;
//            case NSStreamEvent.HasBytesAvailable:
//                Console.WriteLine("StreamEventHasBytesAvailable");
//                ReadData();
//                break;
//            case NSStreamEvent.HasSpaceAvailable:
//                Console.WriteLine("StreamEventHasSpaceAvailable");
//                // Do write operations to the device here
//                break;
//            case NSStreamEvent.OpenCompleted:
//                Console.WriteLine("StreamEventOpenCompleted");
//                break;
//            case NSStreamEvent.ErrorOccurred:
//                Console.WriteLine("StreamEventErroOccurred");
//                break;
//            case NSStreamEvent.EndEncountered:
//                Console.WriteLine("StreamEventEndEncountered");
//                break;
//            default:
//                Console.WriteLine("Stream present but no event");
//                break;