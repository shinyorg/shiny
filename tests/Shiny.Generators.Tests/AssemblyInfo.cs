using Shiny.Generators.Tests;

[assembly: Shiny.GenerateStartup]
[assembly: Shiny.GenerateStaticClasses]
[assembly: Shiny.ShinyInject(typeof(ITestService), typeof(TestService))]
