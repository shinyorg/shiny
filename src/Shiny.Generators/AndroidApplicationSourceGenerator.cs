using System;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    [Generator]
    public class AndroidApplicationSourceGenerator : ShinyApplicationSourceGenerator
    {
        public AndroidApplicationSourceGenerator() : base("Android.App.Application") { }


        protected override void Process(IShinyContext context, INamedTypeSymbol osAppTypeSymbol)
        {
            // TODO: should we try to change it or error right here?
            // TODO: what if not partial?  why did user mark the assembly then?

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
