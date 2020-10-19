using System;
using Uno.SourceGeneration;
using System.Collections.Generic;
using Shiny.Generators.Tasks;
using Shiny.Generators.Tasks.iOS;
using Shiny.Generators.Tasks.Android;
using System.Threading.Tasks;


namespace Shiny.Generators
{
    public class ShinySourceGenerator : SourceGenerator
    {
        readonly IList<ShinySourceGeneratorTask> tasks = new List<ShinySourceGeneratorTask>
        {
            new AutoStartupTask(),
            new StaticClassTask(),
            //new PrismBridgeTask(),
            //new BleClientTask(),
            //new BleHostingHubTask(),

            new AppDelegateTask(),
            new ApplicationTask(),
            new ActivityTask()
        };


        public override void Execute(SourceGeneratorContext context)
        {
            var shinyContext = new ShinyContext(context);

            // always first
            new AutoStartupTask().Init(shinyContext);

            var tasks = new List<Task>();
            foreach (var task in this.tasks)
            {
                tasks.Add(Task.Run(() =>
                {
                    task.Init(shinyContext);
                    task.Execute();
                }));
            }
            Task.WhenAll(tasks.ToArray()).GetAwaiter().GetResult();
        }
    }
}
