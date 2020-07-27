using System;
using Uno.SourceGeneration;
using System.Collections.Generic;
using Shiny.Generators.Tasks;
using Shiny.Generators.Tasks.iOS;
using Shiny.Generators.Tasks.Android;


namespace Shiny.Generators
{
    public class ShinySourceGenerator : SourceGenerator
    {
        readonly IList<ShinySourceGeneratorTask> tasks = new List<ShinySourceGeneratorTask>
        {
            new AutoStartupTask(),
            new StaticClassTask(),
            new PrismBridgeTask(),
            new BleClientTask(),

            new AppDelegateTask(),
            new ApplicationTask(),

            new ActivityTask()
        };


        public override void Execute(SourceGeneratorContext context)
        {
            var shinyContext = new ShinyContext(context);

            foreach (var task in this.tasks)
            {
                task.Init(shinyContext);
                task.Execute();
            }
        }
    }
}
