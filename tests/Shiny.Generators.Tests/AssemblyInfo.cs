using Shiny.Generators;
using Shiny.Generators.Tests;

[assembly: Shiny.Generators.GenerateStartup]
[assembly: Shiny.Generators.GenerateStaticClasses]
[assembly: Shiny.Generators.ShinyInject(typeof(ITestService), typeof(TestService))]
