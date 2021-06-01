using System;
using Cake.Frosting;
using ShinyBuild.Tasks.Library;


namespace ShinyBuild.Tasks
{
    [TaskName("Default")]
    [IsDependentOn(typeof(CopyArtifactsTask))]
    [IsDependentOn(typeof(NugetDeployTask))]
    [IsDependentOn(typeof(DocTask))]
    [IsDependentOn(typeof(TweetVersionTask))]
    public sealed class DefaultTarget : FrostingTask<BuildContext> { }
}
