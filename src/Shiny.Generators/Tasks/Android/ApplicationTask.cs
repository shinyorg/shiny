using System;
using Uno.RoslynHelpers;


namespace Shiny.Generators.Tasks.Android
{
    public class ApplicationTask : ShinySourceGeneratorTask
    {
        public override void Execute()
        {
            // TODO: detect Shiny.Core and Android v10
            // TODO: detect custom application
            // TODO: if application implementations already exist and is it partial?

            // if application exists, error or override? - could also search for attribute?
            var appClass = this.Context.Compilation.GetTypeByMetadataName("Android.App.Application");
            if (appClass == null)
                return;

            //var implementations = this.Context.GetAllImplementationsOfType(appClass);
            if (!appClass.IsPartialClass())
            {
                this.Log.Warn("Cannot generate Shiny application class since your custom Application is not marked as partial");
                return;
            }
            //// TODO: log error if android application already exists? only gen it if there isn't an existing one?

            var startupClass = this.ShinyContext.GetShinyStartupClassFullName();
            if (startupClass == null)
                return;

            var ns = this.ShinyContext.GetRootNamespace();
            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces("Android.App", "Android.Content", "Android.Runtime");

            using (builder.BlockInvariant($"namespace {ns}"))
            {
                builder.AppendLineInvariant("[ApplicationAttribute]");
                using (builder.BlockInvariant("public partial class AppShinyApplication : Application"))
                {
                    builder.AppendLine("public AppShinyApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) {}");
                    builder.AppendLine();

                    using (builder.BlockInvariant("public override void OnCreate()"))
                    {
                        builder.AppendLineInvariant($"AndroidShinyHost.Init(this, new {startupClass}());");

                        if (this.Context.HasXamarinEssentials())
                            builder.AppendLineInvariant("Xamarin.Essentials.Platform.Init(this);");

                        if (this.Context.Compilation.GetTypeByMetadataName("Acr.UserDialogs.UserDialogs") != null)
                            builder.AppendLineInvariant("Acr.UserDialogs.UserDialogs.Init(this);");

                        builder.AppendLineInvariant("base.OnCreate();");
                    }

                    using (builder.BlockInvariant("public override void OnTrimMemory([GeneratedEnum] TrimMemory level)"))
                    {
                        builder.AppendLineInvariant("AndroidShinyHost.OnBackground(level);");
                        builder.AppendLineInvariant("base.OnTrimMemory(level);");
                    }
                }
            }
            this.Context.AddCompilationUnit("MainApplication", builder.ToString());
        }
    }
}