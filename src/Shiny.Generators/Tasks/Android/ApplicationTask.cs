using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.RoslynHelpers;


namespace Shiny.Generators.Tasks.Android
{
    public class ApplicationTask : ShinySourceGeneratorTask
    {
        public override void Execute()
        {
            if (!this.Context.IsAndroidAppProject())
                return;

            var major = this.Context.GetAndroidMajorTarget();
            if (major < 10)
                this.Log.Error("You must target a minimum major target of Android 10 to use Shiny");

            var startupClass = this.ShinyContext.GetShinyStartupClassFullName();
            if (startupClass == null)
                return;

            var appImpls = this.Context
                .GetAllDerivedClassesForType("Android.App.Application", true)
                .WhereNotSystem()
                .ToList();

            if (!appImpls.Any())
            {
                this.GenerateFromScratch(startupClass);
            }
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
                }
            }
            this.Context.AddCompilationUnit("MainApplication", builder.ToString());
        }


        void GeneratePartial(INamedTypeSymbol symbol, string startupClassName)
        {
            var hasOnCreate = symbol.HasMethod("OnCreate");
            if (hasOnCreate)
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
                }
            }
            this.Context.AddCompilationUnit(symbol.Name, builder.ToString());
        }


        void AppendOnCreate(IndentedStringBuilder builder, string startupClassName)
        {
            using (builder.BlockInvariant("public override void OnCreate()"))
            {
                builder.AppendLineInvariant($"this.ShinyOnCreate(new {startupClassName}());");

                if (this.Context.HasXamarinEssentials())
                    builder.AppendLineInvariant("global::Xamarin.Essentials.Platform.Init(this);");

                if (this.Context.Compilation.GetTypeByMetadataName("Acr.UserDialogs.UserDialogs") != null)
                    builder.AppendLineInvariant("global::Acr.UserDialogs.UserDialogs.Init(this);");

                builder.AppendLineInvariant("base.OnCreate();");
            }
        }
    }
}