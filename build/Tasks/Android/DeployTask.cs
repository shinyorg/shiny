using System;
using Cake.AppCenter;
using Cake.Frosting;


namespace ShinyBuild.Tasks.Android
{
    [TaskName("AndroidSample")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class DeployTask : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
            => context.IsRunningInCI && !context.IsPullRequest;


        public override void Run(BuildContext context)
        {
            context.AppCenterDistributeGroupsPublish(new AppCenterDistributeGroupsPublishSettings
            {
            });
        }
    }
}
