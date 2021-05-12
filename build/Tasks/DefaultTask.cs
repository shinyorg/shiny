using System;
using Cake.Frosting;


namespace ShinyBuild.Tasks
{
    [TaskName("Default")]
    //[IsDependentOn(typeof(SampleBuildTask))]
    [IsDependentOn(typeof(NugetDeployTask))]
    [IsDependentOn(typeof(DocTask))]
    public sealed class DefaultTask : FrostingTask<BuildContext> { }
}
