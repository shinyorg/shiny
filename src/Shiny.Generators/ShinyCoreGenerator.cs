using System;
using System.Collections.Generic;
using Shiny.Generators.Tasks;
//using Shiny.Generators.Tasks.iOS;
//using Shiny.Generators.Tasks.Android;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    [Generator]
    public class ShinyCoreGenerator : ISourceGenerator
    {
        readonly IList<ShinySourceGeneratorTask> tasks = new List<ShinySourceGeneratorTask>
        {
            ////new PrismBridgeTask(),

        };


        public void Execute(GeneratorExecutionContext context)
        {
            if (context.Compilation.Language != LanguageNames.CSharp)
                return;

            // retreive the populated receiver
            //if (!(context.SyntaxReceiver is ShinySyntaxReceiver receiver))
            //    return;

            //var workspace = Workspace.GetWorkspaceRegistration(context.Compilation.);
            //workspace.Workspace.Kind == WorkspaceKind.MSBuild
            //workspace.Workspace.CurrentSolution.Projects.
            var shinyContext = new ShinyContext(context);

            // always first
            var autoStartup = new AutoStartupTask();
            this.RunTask(autoStartup, shinyContext).GetAwaiter().GetResult();

            //Task
            //    .WhenAll(
            //        this.RunTask(new AppDelegateTask(), shinyContext),
            //        this.RunTask(new ApplicationTask(), shinyContext),
            //        this.RunTask(new ActivityTask(), shinyContext),
            //        shinyContext.Context.CancellationToken
            //    )
            //    .GetAwaiter()
            //    .GetResult();
        }


        async Task RunTask(ShinySourceGeneratorTask task, IShinyContext context)
        {
            try
            {
                task.Init(context);
                task.Execute();
            }
            catch (Exception ex)
            {
                //shinyContext.Log.Warn($"{task.GetType().FullName} Exception - {ex}");
            }
        }

        public void Initialize(GeneratorInitializationContext context)
        {
            // Register a syntax receiver that will be created for each generation pass
            context.RegisterForSyntaxNotifications(() => new ShinySyntaxReceiver());
        }
    }
}
