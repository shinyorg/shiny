using System;
using Shiny.Generators.Tasks;
using Uno.RoslynHelpers;


namespace Shiny.Generators.Tasks.Android
{
    public class ApplicationTask : ShinySourceGeneratorTask
    {
        public override void Execute()
        {
            // if application exists, error or override? - could also search for attribute?
            var appClass = this.Context.Compilation.GetTypeByMetadataName("Android.App.Application");
            if (appClass == null)
                return;

            // TODO: log error if android application already exists? only gen it if there isn't an existing one?

            var startupClass = this.Context.GetShinyStartupSymbol();
            if (startupClass == null)
                return;

            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces("Android.App", "Android.Runtime");

            using (builder.BlockInvariant($"namespace {this.Context.Compilation.GlobalNamespace.Name}"))
            {
                builder.AppendLineInvariant("[ApplicationAttribute]");
                using (builder.BlockInvariant("public partial class AppShinyApplication : Application"))
                {
                    builder.AppendLineInvariant("public AppShinyApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) {}");
                    using (builder.BlockInvariant("public override void OnCreate()"))
                    {
                        builder.AppendLineInvariant($"this.InitShiny({startupClass.ToDisplayString()});");

                        if (this.Context.Compilation.GetTypeByMetadataName("Xamarin.Essentials.Platform") != null)
                            builder.AppendLine("Xamarin.Essentials.Platform.Init(this);");

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