using System;


namespace Shiny.Attributes
{
    // TODO: IsPushLibrary, UsesBackgroundTransfer
    //this.RegisterAllDelegate("Shiny.Locations.Sync.IGeofenceSyncDelegate", "services.UseGeofencingSync", true);
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
    public class AutoStartupAttribute : Attribute
    {
        // if no delegates and required, don't register
        public AutoStartupAttribute(string interfaceTypeName, string startupRegistration, bool delegateRequired) { }
    }
}
