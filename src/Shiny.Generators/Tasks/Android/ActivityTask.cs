using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.RoslynHelpers;


namespace Shiny.Generators.Tasks.Android
{
    public class ActivityTask : ShinySourceGeneratorTask
    {
        public override void Execute()
        {
            if (!this.Context.IsAndroidAppProject())
                return;

            this.Iterate("AndroidX.AppCompat.App.AppCompatActivity");
            this.Iterate("Android.Support.V7.App.AppCompatActivity");
        }


        void Iterate(string activityType)
        {
            var activities = this
                .Context
                .GetAllDerivedClassesForType(activityType)
                .WhereNotSystem()
                .ToList();

            foreach (var activity in activities)
            {
                this.GenerateActivity(activity);
            }
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
            this.Context.AddCompilationUnit(activity.Name, builder.ToString());
        }


        void TryAppendOnCreate(INamedTypeSymbol activity, IIndentedStringBuilder builder)
        {
            if (!activity.HasMethod("OnCreate"))
            {
                using (builder.BlockInvariant("protected override void OnCreate(Bundle savedInstanceState)"))
                {
                    // Xamarin Forms
                    if (activity.Is("Xamarin.Forms.Platform.Android.FormsAppCompatActivity"))
                    {
                        var appClass = this.ShinyContext.GetXamFormsAppClassFullName();
                        if (appClass != null)
                        {
                            builder.AppendLineInvariant("TabLayoutResource = Resource.Layout.Tabbar;");
                            builder.AppendLineInvariant("ToolbarResource = Resource.Layout.Toolbar;");
                            builder.AppendLineInvariant("base.OnCreate(savedInstanceState);");
                            builder.AppendLineInvariant("global::Xamarin.Forms.Forms.Init(this, savedInstanceState);");
                            builder.AppendLineInvariant($"this.LoadApplication(new {appClass}());");
                        }
                    }
                    else
                    {
                        builder.AppendLineInvariant("base.OnCreate(savedInstanceState);");
                        this.AppendShinyOnCreate(activity, builder);
                    }
                    this.TryAppendOnCreateThirdParty(activity, builder);
                }
            }
        }


        void AppendShinyOnCreate(INamedTypeSymbol activity, IIndentedStringBuilder builder)
        {
            builder.AppendLineInvariant("this.ShinyOnCreate();");
            if (activity.HasMethod("OnCreated"))
                builder.AppendLineInvariant("this.OnCreated();");
        }


        void TryAppendOnCreateThirdParty(INamedTypeSymbol activity, IIndentedStringBuilder builder)
        {


            // AiForms.SettingsView
            if (this.Context.Compilation.GetTypeByMetadataName("AiForms.Renderers.Droid.SettingsViewInit") != null)
                builder.AppendFormatInvariant("global::AiForms.Renderers.Droid.SettingsViewInit.Init();");

            // XF Material
            if (this.Context.Compilation.GetTypeByMetadataName("XF.Material.Forms.Material") != null)
                builder.AppendFormatInvariant("global::XF.Material.Droid.Material.Init(this, savedInstanceState);");
        }


        void TryAppendRequestPermissionResult(INamedTypeSymbol activity, IIndentedStringBuilder builder)
        {
            if (!activity.HasMethod("OnRequestPermissionsResult"))
            {
                using (builder.BlockInvariant("public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)"))
                {
                    builder.AppendLine("base.OnRequestPermissionsResult(requestCode, permissions, grantResults);");
                    builder.AppendLine();
                    builder.AppendLine("this.ShinyOnRequestPermissionsResult(requestCode, permissions, grantResults);");
                    builder.AppendLine();

                    if (this.Context.HasXamarinEssentials())
                        builder.AppendLineInvariant("global::Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);");

                    //if (this.Context.HasZXingNetMobile())
                    //    builder.AppendLineInvariant("global::ZXing.Net.Mobile.Android.PermissionsHandler.OnRequestPermissionsResult(requestCode, permissions, grantResults);");
                }
            }
        }


        void TryAppendNewIntent(INamedTypeSymbol activity, IIndentedStringBuilder builder)
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