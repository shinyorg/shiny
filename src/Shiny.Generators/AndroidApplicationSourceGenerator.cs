using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
//using Microsoft.CodeAnalysis.CSharp;
//using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;


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

            var nameSpace = this.Context.GetRootNamespace();
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
            var source = builder.ToString();
            this.Context.AddSource(ApplicationName, source);
        }


        void AppendOnCreate(IndentedStringBuilder builder)
        {
            using (builder.BlockInvariant("public override void OnCreate()"))
            {
                builder.AppendLineInvariant($"this.ShinyOnCreate(new global::{this.ShinyConfig.ShinyStartupTypeName}());");

                if (this.Context.HasXamarinEssentials())
                    builder.AppendLineInvariant("global::Xamarin.Essentials.Platform.Init(this);");

                foreach (var lib in Constants.AndroidApplicationThirdPartyRegistrations)
                {
                    if (this.Context.Compilation.GetTypeByMetadataName(lib.Key) != null)
                        builder.AppendLineInvariant(lib.Value);
                }
                builder.AppendLineInvariant("base.OnCreate();");
            }
        }
    }
}
