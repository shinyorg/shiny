using System;


namespace Shiny
{
    public interface IEnvironment
    {
        string AppIdentifier { get; }
        string AppVersion { get; }
        string AppBuild { get; }

        string MachineName { get; }
        string OperatingSystem { get; }
        string OperatingSystemVersion { get; }
        string Manufacturer { get; }
        string Model { get; }

        //void IsVersionOrHigher(int version - string version, use switch)
        //bool HasHardware(bluetooth, network, etc)
    }
}
