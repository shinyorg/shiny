using System;


namespace Acr
{
    public class Environment : IEnvironment
    {
        public string AppIdentifier { get; }
        public string AppVersion { get; }
        public string AppBuild { get; }
        public string MachineName { get; }
        public string OperatingSystem { get; }
        public string Manufacturer { get; }
        public string Model { get; }
    }
}
