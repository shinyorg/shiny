using System;
using System.Diagnostics;
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
                    if (!activity.HasMethod("OnCreate"))
                    {
                        using (builder.BlockInvariant("protected override void OnCreate(Bundle savedInstanceState)"))
                        {
                            builder.AppendLineInvariant("base.OnCreate(savedInstanceState);");
                            builder.AppendLineInvariant("this.ShinyOnCreate();");
                            if (activity.HasMethod("OnCreated"))
                                builder.AppendLineInvariant("this.OnCreated()");

                            if (activity.Is("Xamarin.Forms.Platform.Android.FormsAppCompatActivity"))
                            {
                                var appClass = this.ShinyContext.GetXamFormsAppClassFullName();
                                if (appClass != null)
                                {
                                    builder.AppendLineInvariant("TabLayoutResource = Resource.Layout.Tabbar;");
                                    builder.AppendLineInvariant("ToolbarResource = Resource.Layout.Toolbar;");
                                    builder.AppendLineInvariant("global::Xamarin.Forms.Forms.Init(this, savedInstanceState);");
                                    builder.AppendLineInvariant($"this.LoadApplication(new {appClass}());");
                                }
                            }
                        }
                    }

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

                    if (!activity.HasMethod("OnRequestPermissionsResult"))
                    {
                        using (builder.BlockInvariant("public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)"))
                        {
                            builder.AppendLine("base.OnRequestPermissionsResult(requestCode, permissions, grantResults);");
                            builder.AppendLine();
                            builder.AppendLine("this.ShinyOnRequestPermissionsResult(requestCode, permissions, grantResults);");
                            builder.AppendLine();

                            if (this.Context.HasXamarinEssentials())
                                builder.AppendLineInvariant("Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);");
                        }
                    }
                }
            }
            this.Context.AddCompilationUnit(activity.Name, builder.ToString());
        }
    }
}