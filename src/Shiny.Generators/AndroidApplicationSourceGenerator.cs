using System;
using System.Linq;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    [Generator]
    public class AndroidApplicationSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            if (!context.CurrentAssemblyHasAttribute(Constants.ShinyApplicationAttributeTypeName))
                return;

            var androidAppType = context.Compilation.GetTypeByMetadataName("Android.App.Application");
            if (androidAppType == null)
                return;

            var androidApplications = context
                .Compilation
                .Assembly
                .GetAllTypeSymbols()
                .Where(x => x.Inherits(androidAppType))
                .ToList();

            foreach (var androidApp in androidApplications)
            {
                // TODO: should we try to change it or error right here?
                // TODO: what if not partial?  why did user mark the assembly then?
            }
        }


        public void Initialize(GeneratorInitializationContext context) { }


        void GenerateFromScratch()
        {
            //var ns = this.ShinyContext.GetRootNamespace();
            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces("Android.App", "Android.Content", "Android.Runtime");

            // TODO: startup class is either being added here or generated on the application
            // TODO: I need the root namespace... could just use the assembly globalnamespace?
            using (builder.BlockInvariant($"namespace "))
            {
                builder.AppendLineInvariant("[ApplicationAttribute]");
                using (builder.BlockInvariant("public partial class MainApplication : Application"))
                {
                    builder.AppendLine("public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) {}");
                    builder.AppendLine();

                    //this.AppendOnCreate(builder, startupClassName);
                }
            }
            //this.Context.AddSource("MainApplication", builder.ToString());
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
                        //this.Log.Warn($"Cannot generate OnCreate method for {symbol.Name} since it already exists.  Make sure to call Shiny.AndroidShinyHost.Init(new YourShinyStartup()); in your OnCreate");
                    }
                    else
                    {
                        this.AppendOnCreate(builder, startupClassName);
                    }
                }
            }
            //this.Context.AddSource(symbol.Name, builder.ToString());
        }


        void AppendOnCreate(IndentedStringBuilder builder, string startupClassName)
        {
            using (builder.BlockInvariant("public override void OnCreate()"))
            {
                builder.AppendLineInvariant($"this.ShinyOnCreate(new {startupClassName}());");

                //if (this.Context.HasXamarinEssentials())
                //    builder.AppendLineInvariant("global::Xamarin.Essentials.Platform.Init(this);");

                //if (this.Context.Compilation.GetTypeByMetadataName("Acr.UserDialogs.UserDialogs") != null)
                //    builder.AppendLineInvariant("global::Acr.UserDialogs.UserDialogs.Init(this);");

                builder.AppendLineInvariant("base.OnCreate();");
            }
        }
    }
}
