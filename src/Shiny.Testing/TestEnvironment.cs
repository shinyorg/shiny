using System;


namespace Shiny.Testing
{
    public class TestEnvironment : IEnvironment
    {
        public string AppIdentifier { get; set; } = "UnitTests";
        public string AppVersion { get; set; } = "0.1";
        public string AppBuild { get; set; } = "0.0";
        public string MachineName { get; set; } = "Testing";
        public string OperatingSystem { get; set; } = "Windows";
        public string Manufacturer { get; set; } = "You";
        public string Model { get; set; } = "Computer";
    }
}
