using System;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;


namespace Shiny.Generators.Generators.Android
{
    public static class ApplicationSourceGenerator
    {
        public static void Execute(SourceGeneratorContext context)
        {
            // if application exists, error or override? - could also search for attribute?
            var appClass = context.Compilation.GetTypeByMetadataName("Android.App.Application");
            if (appClass == null)
                return;

            var builder = new IndentedStringBuilder();
            builder.AppendLineInvariant("System;");
            builder.AppendLineInvariant("Android.App;");
            builder.AppendLineInvariant("Shiny;");

            using (builder.BlockInvariant($"namespace {context.Compilation.GlobalNamespace.Name}"))
            {
                builder.AppendLineInvariant("[ApplicationAttribute]");
                using (builder.BlockInvariant("public class AppShinyApplication : Application"))
                {
                    builder.AppendLineInvariant("public AppShinyApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) {}");
                    using (builder.BlockInvariant("public override void OnCreate()"))
                    {
                        builder.AppendLineInvariant("this.InitShiny();");
                        builder.AppendLineInvariant("base.OnCreate();");
                    }

                    using (builder.BlockInvariant("public override void OnTrimMemory([GeneratedEnum] TrimMemory level)"))
                    {
                        builder.AppendLineInvariant("AndroidShinyHost.OnBackground(level);");
                        builder.AppendLineInvariant("base.OnTrimMemory(level);");
                    }
                }
            }
        }
    }
}