using System;
using System.IO;
using System.Reactive.Subjects;


namespace Shiny.Testing
{
    public class TestPlatform : IPlatform
    {
        public TestPlatform()
        {
            this.AppData = this.Cache = this.Public = new DirectoryInfo(".");
        }



        public string Name => "Test";
        public PlatformState Status { get; set; } = PlatformState.Foreground;
        public DirectoryInfo AppData { get; set; }
        public DirectoryInfo Cache { get; set; }
        public DirectoryInfo Public { get; set; }

        public string AppIdentifier { get; set; } = "UnitTests";
        public string AppVersion { get; set; } = "0.1";
        public string AppBuild { get; set; } = "0.0";
        public string MachineName { get; set; } = "Testing";
        public string OperatingSystem { get; set; } = "Windows";
        public string OperatingSystemVersion { get; set; } = "1";
        public string Manufacturer { get; set; } = "You";
        public string Model { get; set; } = "Computer";

        public Subject<PlatformState> PlatformSubject { get; } = new Subject<PlatformState>();

        public void InvokeOnMainThread(Action action) => action();
        public virtual IObservable<PlatformState> WhenStateChanged()
            => this.PlatformSubject;
    }
}
