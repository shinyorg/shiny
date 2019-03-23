using System;
using Shiny.Settings;


namespace Shiny.Device.Tests.Settings
{

    public class DefaultSettingTests : AbstractSettingTests
    {
        public DefaultSettingTests()
        {
            this.Settings = ShinyHost.Resolve<ISettings>();
        }
    }
}
