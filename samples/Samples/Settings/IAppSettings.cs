using System;


namespace Samples.Settings
{
    public interface IAppSettings
    {
        bool IsChecked { get; set; }
        string YourText { get; set; }
        DateTime? LastUpdated { get; set; }
    }
}
