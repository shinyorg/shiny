using System;
using System.Reflection;
using Xunit.Runners.UI;


namespace Shiny.Tests.Uwp
{
    sealed partial class App : RunnerApplication
    {
        public App()
        {
            this.ShinyInit(new TestStartup());
        }


        protected override void OnInitializeRunner()
        {
            this.AddTestAssembly(this.GetType().GetTypeInfo().Assembly);
            this.AddTestAssembly(typeof(TestStartup).Assembly);
        }
    }
}
