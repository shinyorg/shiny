using System;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;

using Uno.RoslynHelpers;
using Uno.SourceGeneration;

namespace Shiny.Generators.Tasks.Android
{
    public class ApplicationTask : ShinySourceGeneratorTask
    {
        public override void Execute()
        {
            // TODO: is android head?
            // TODO: detect Android v10

            var appClass = this.Context.Compilation.GetTypeByMetadataName("Android.App.Application");
            if (appClass == null)
                return; // no android class

            var startupClass = this.ShinyContext.GetShinyStartupClassFullName();
            if (startupClass == null)
                return;

            var appImpls = this.Context
                .GetAllDerivedClassesForType("Android.App.Application", true)
                .WhereNotSystem()
                .ToList();

            if (!appImpls.Any())
                this.GenerateFromScratch(startupClass);
            else
            {
                foreach (var impl in appImpls)
                    this.GeneratePartial(impl, startupClass);
            }
        }


        void GenerateFromScratch(string startupClassName)
        {
            var ns = this.ShinyContext.GetRootNamespace();
            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces("Android.App", "Android.Content", "Android.Runtime");

            using (builder.BlockInvariant($"namespace {ns}"))
            {
                builder.AppendLineInvariant("[ApplicationAttribute]");
                using (builder.BlockInvariant("public partial class MainApplication : Application"))
                {
                    builder.AppendLine("public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) {}");
                    builder.AppendLine();

                    this.AppendOnCreate(builder, startupClassName);
                    this.AppendOnTrimMemory(builder);
                }
            }
            this.Context.AddCompilationUnit("MainApplication", builder.ToString());
        }


        void GeneratePartial(INamedTypeSymbol symbol, string startupClassName)
        {
            var hasOnCreate = symbol.HasMethod("OnCreate");
            var hasTrim = symbol.HasMethod("OnTrimMemory");

            if (hasOnCreate && hasTrim)
                return;

            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces("Android.App", "Android.Content", "Android.Runtime");

            using (builder.BlockInvariant("namespace " + symbol.ContainingNamespace.ToDisplayString()))
            {
                using (builder.BlockInvariant("public partial class " + symbol.Name))
                {
                    if (hasOnCreate)
                    {
                        this.Log.Warn($"Cannot generate OnCreate method for {symbol.Name} since it already exists.  Make sure to call Shiny.AndroidShinyHost.Init(new YourShinyStartup()); in your OnCreate");
                    }
                    else
                    {
                        this.AppendOnCreate(builder, startupClassName);
                    }

                    if (hasTrim)
                    {
                        this.Log.Warn($"Cannot generate OnTrimMemory method for {symbol.Name} since it already exists.  Make sure to call Shiny.AndroidShinyHost.OnBackground(level); in your OnTrimMemory");
                    }
                    else
                    {
                        this.AppendOnTrimMemory(builder);
                    }
                }
            }
            this.Context.AddCompilationUnit(symbol.Name, builder.ToString());
        }


        void AppendOnCreate(IndentedStringBuilder builder, string startupClassName)
        {
            using (builder.BlockInvariant("public override void OnCreate()"))
            {
                builder.AppendLineInvariant($"AndroidShinyHost.Init(this, new {startupClassName}());");

                if (this.Context.HasXamarinEssentials())
                    builder.AppendLineInvariant("Xamarin.Essentials.Platform.Init(this);");

                if (this.Context.Compilation.GetTypeByMetadataName("Acr.UserDialogs.UserDialogs") != null)
                    builder.AppendLineInvariant("Acr.UserDialogs.UserDialogs.Init(this);");

                builder.AppendLineInvariant("base.OnCreate();");
            }
        }


        void AppendOnTrimMemory(IndentedStringBuilder builder)
        {
            using (builder.BlockInvariant("public override void OnTrimMemory([GeneratedEnum] TrimMemory level)"))
            {
                builder.AppendLineInvariant("AndroidShinyHost.OnBackground(level);");
                builder.AppendLineInvariant("base.OnTrimMemory(level);");
            }
        }
    }
}