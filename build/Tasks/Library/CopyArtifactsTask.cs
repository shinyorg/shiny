using System;
using Cake.Common.IO;
using Cake.Frosting;


namespace ShinyBuild.Tasks.Library
{
    [IsDependentOn(typeof(BuildTask))]
    public class CopyArtifactsTask : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
            => context.IsRunningInCI;


        public override void Run(BuildContext context)
        {
            context.DeleteFiles("src/**/*.symbols.nupkg");
            var directory = context.Directory(context.ArtifactDirectory);
            context.CopyFiles("src/**/*.nupkg", directory);
        }
    }
}
