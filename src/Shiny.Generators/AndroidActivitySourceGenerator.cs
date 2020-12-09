using System;
using System.Linq;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    [Generator]
    public class AndroidActivitySourceGenerator : ISourceGenerator
    {
        GeneratorExecutionContext context;
        ShinyApplicationValues values;


        public void Execute(GeneratorExecutionContext context)
        {
            this.context = context;
            var attributeData = context.GetCurrentAssemblyAttribute(Constants.ShinyApplicationAttributeTypeName);

            if (attributeData != null)
            {
                this.values = new ShinyApplicationValues(attributeData);
                this.Iterate("AndroidX.AppCompat.App.AppCompatActivity");
                this.Iterate("Android.Support.V7.App.AppCompatActivity");
            }
        }


        public void Initialize(GeneratorInitializationContext context) { }


        void Iterate(string activityTypeName)
        {
            var activityType = this.context.Compilation.GetTypeByMetadataName(activityTypeName);
            if (activityType == null)
                return;

            //if (!activityType.IsPartialClass())

            var activities = this
                .context
                .Compilation
                .Assembly
                .GetAllTypeSymbols()
                .Where(x => x.Inherits(activityType))
                .ToList();

            foreach (var activity in activities)
                this.GenerateActivity(activity);
        }


        void GenerateActivity(INamedTypeSymbol activity)
        {
            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces(
                "Android.App",
                "Android.Content",
                "Android.Content.PM",
                "Android.OS",
                "Android.Runtime"
            );

            using (builder.BlockInvariant($"namespace {activity.ContainingNamespace}"))
            {
                using (builder.BlockInvariant($"public partial class {activity.Name}"))
                {
                    this.TryAppendOnCreate(activity, builder);
                    this.TryAppendNewIntent(activity, builder);
                    this.TryAppendRequestPermissionResult(activity, builder);
                }
            }
            this.context.AddSource(activity.Name, builder.ToString());
        }


        void TryAppendOnCreate(INamedTypeSymbol activity, IndentedStringBuilder builder)
        {
            if (!activity.HasMethod("OnCreate"))
            {
                using (builder.BlockInvariant("protected override void OnCreate(Bundle savedInstanceState)"))
                {
                    builder.AppendLineInvariant("this.ShinyOnCreate();");
                    if (activity.HasMethod("OnCreating"))
                        builder.AppendLineInvariant("this.OnCreating(savedInstanceState);");

                    if (String.IsNullOrWhiteSpace(this.values.XamarinFormsAppTypeName))
                    {
                        builder.AppendFormatInvariant("base.OnCreate(savedInstanceState);");
                    }
                    else
                    {
                        var xfFormsActivityType = context.Compilation.GetTypeByMetadataName("Xamarin.Forms.Platform.Android.FormsAppCompatActivity");
                        if (xfFormsActivityType != null && activity.Inherits(xfFormsActivityType))
                        {
                            // do XF stuff
                            builder.AppendLineInvariant("TabLayoutResource = Resource.Layout.Tabbar;");
                            builder.AppendLineInvariant("ToolbarResource = Resource.Layout.Toolbar;");
                            builder.AppendLineInvariant("base.OnCreate(savedInstanceState);");
                            builder.AppendLineInvariant("global::Xamarin.Forms.Forms.Init(this, savedInstanceState);");
                            builder.AppendLineInvariant($"this.LoadApplication(new {this.values.XamarinFormsAppTypeName}());");
                        }
                    }
                    //this.TryAppendOnCreateThirdParty(activity, builder);
                }
            }
        }


        //void TryAppendOnCreateThirdParty(INamedTypeSymbol activity, IndentedStringBuilder builder)
        //{
        //    // AiForms.SettingsView
        //    if (this.Context.Compilation.GetTypeByMetadataName("AiForms.Renderers.Droid.SettingsViewInit") != null)
        //        builder.AppendLineInvariant("global::AiForms.Renderers.Droid.SettingsViewInit.Init();");

        //    // XF Material
        //    if (this.Context.Compilation.GetTypeByMetadataName("XF.Material.Forms.Material") != null)
        //        builder.AppendLineInvariant("global::XF.Material.Droid.Material.Init(this, savedInstanceState);");
        //    else if (this.Context.Compilation.GetTypeByMetadataName("Rg.Plugins.Popup.Popup") != null)
        //        builder.AppendLineInvariant("global::Rg.Plugins.Popup.Popup.Init(this, savedInstanceState);");
        //}


        void TryAppendRequestPermissionResult(INamedTypeSymbol activity, IndentedStringBuilder builder)
        {
            if (!activity.HasMethod("OnRequestPermissionsResult"))
            {
                using (builder.BlockInvariant("public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)"))
                {
                    builder.AppendLine("base.OnRequestPermissionsResult(requestCode, permissions, grantResults);");
                    builder.AppendLine();
                    builder.AppendLine("this.ShinyOnRequestPermissionsResult(requestCode, permissions, grantResults);");
                    builder.AppendLine();

                    if (this.context.HasXamarinEssentials())
                        builder.AppendLineInvariant("global::Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);");
                }
            }
        }


        void TryAppendNewIntent(INamedTypeSymbol activity, IndentedStringBuilder builder)
        {
            if (!activity.HasMethod("OnNewIntent"))
            {
                using (builder.BlockInvariant("protected override void OnNewIntent(Intent intent)"))
                {
                    builder.AppendLine("base.OnNewIntent(intent);");
                    builder.AppendLine();
                    builder.AppendLine("this.ShinyOnNewIntent(intent);");
                    builder.AppendLine();
                }
            }
        }
    }
}
