using System;
using System.Reflection;
using Xunit.Runners.UI;


namespace Shiny.Device.Tests.Uwp
{
    sealed partial class App : RunnerApplication
    {
        protected override void OnInitializeRunner()
        {
            this.ShinyInit(new TestStartup());
            this.AddTestAssembly(this.GetType().GetTypeInfo().Assembly);
        }
    }
}
