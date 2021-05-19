using System;
using Cake.AndroidAppManifest;
using Cake.Frosting;


namespace ShinyBuild.Tasks.Android
{
    public sealed class ManifestTask : FrostingTask<BuildContext>
    {
        const string DroidManifest = "samples/Samples.Android/Properties/AndroidManifest.xml";


        public override bool ShouldRun(BuildContext context)
            => context.IsRunningInCI;


        public override void Run(BuildContext context)
        {
            var manifest = context.DeserializeAppManifest(DroidManifest);
            manifest.VersionName = "1.0";
            manifest.VersionCode = 1;
            manifest.PackageName = "com.shinyorg.samples";
            context.SerializeAppManifest(DroidManifest, manifest);
        }
    }
}
