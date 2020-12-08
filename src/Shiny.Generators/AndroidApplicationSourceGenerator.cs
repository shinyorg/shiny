using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    [Generator]
    public class AndroidApplicationSourceGenerator : ShinyApplicationSourceGenerator
    {
        const string AndroidApplicationTypeName = "Android.App.Application";
        const string ApplicationName = "MainApplication";


        public AndroidApplicationSourceGenerator() : base(AndroidApplicationTypeName) { }


        protected override void Process(IEnumerable<INamedTypeSymbol> osAppTypeSymbols)
        {
            if (osAppTypeSymbols.Any())
                return;

            // TODO: this is supposed to run when there are NO android apps - should we try to change it or error right here?
            // TODO: what if not partial?  why did user mark the assembly then?

            var nameSpace = this.Context.Compilation.AssemblyName;
            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces("Android.App", "Android.Content", "Android.Runtime");

            using (builder.BlockInvariant("namespace " + nameSpace))
            {
                builder.AppendLineInvariant("[global::Android.App.ApplicationAttribute]");
                using (builder.BlockInvariant($"public partial class {ApplicationName} : global::{AndroidApplicationTypeName}"))
                {
                    builder.AppendLine("public MainApplication(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer) {}");
                    builder.AppendLine();

                    this.AppendOnCreate(builder);
                }
            }
            this.Context.Source(builder.ToString(), ApplicationName);
        }



        void AppendOnCreate(IndentedStringBuilder builder)
        {
            using (builder.BlockInvariant("public override void OnCreate()"))
            {
                builder.AppendLineInvariant($"this.ShinyOnCreate(new {this.ShinyConfig.ShinyStartupTypeName}());");

                if (this.Context.HasXamarinEssentials())
                    builder.AppendLineInvariant("global::Xamarin.Essentials.Platform.Init(this);");

                if (this.Context.Compilation.GetTypeByMetadataName("Acr.UserDialogs.UserDialogs") != null)
                    builder.AppendLineInvariant("global::Acr.UserDialogs.UserDialogs.Init(this);");

                builder.AppendLineInvariant("base.OnCreate();");
            }
        }
    }
}
