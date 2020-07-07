using System;
using Uno.SourceGeneration;


namespace Shiny.Generators
{
    //[GenerateAfter("Shiny.Generators.StartupSourceGenerator")]
    public class ActivitySourceGenerator : SourceGenerator
    {
        public override void Execute(SourceGeneratorContext context)
        {
            var project = context.GetProjectInstance();
            var shinyStartupSymbol = context.Compilation.GetTypeByMetadataName(typeof(IShinyStartupTask).FullName);

            if (shinyStartupSymbol is null)
                return;

            //[Activity(
            //    Label = "Shiny",
            //    Icon = "@mipmap/icon",
            //    Theme = "@style/MainTheme",
            //    MainLauncher = true,
            //    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
            //)]
            //public class MainActivity : FormsAppCompatActivity
        
            var activityAttribute = context.Compilation.GetTypeByMetadataName("Android.App.ActivityAttribute");
            if (activityAttribute == null)
                return;
        }
    }
}
//[Activity(
//        Label = "Shiny",
//        Icon = "@mipmap/icon",
//        Theme = "@style/MainTheme",
//        MainLauncher = true,
//        ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation
//    )]
//public class MainActivity : FormsAppCompatActivity
//{
//    protected override void OnCreate(Bundle savedInstanceState)
//    {
//        TabLayoutResource = Resource.Layout.Tabbar;
//        ToolbarResource = Resource.Layout.Toolbar;

//        base.OnCreate(savedInstanceState);

//        Forms.SetFlags(
//            "SwipeView_Experimental",
//            "Expander_Experimental"
//        );
//        Forms.Init(this, savedInstanceState);
//        FormsMaps.Init(this, savedInstanceState);

//        XF.Material.Droid.Material.Init(this, savedInstanceState);
//        this.LoadApplication(new App());

//        this.ShinyOnCreate();
//    }


//    protected override void OnNewIntent(Intent intent)
//    {
//        base.OnNewIntent(intent);
//        this.ShinyOnNewIntent(intent);
//    }


//    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
//    {
//        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
//        this.ShinyRequestPermissionsResult(requestCode, permissions, grantResults);
//    }