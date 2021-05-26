using System;
using System.IO;
using Cake.Common.IO;
using Cake.Frosting;


namespace ShinyBuild.Tasks.Library
{
    [IsDependentOn(typeof(BuildTask))]
    public class CopyArtifactsTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.DeleteFiles("src/**/*.symbols.nupkg");
            var directory = context.Directory(context.ArtifactDirectory);
            if (Directory.Exists(directory.Path.FullPath))
                context.CleanDirectory(directory);
            else
                Directory.CreateDirectory(directory.Path.FullPath);

            context.CopyFiles("src/**/*.nupkg", directory);
        }
    }
}
