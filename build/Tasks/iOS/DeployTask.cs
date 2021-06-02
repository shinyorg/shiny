using System;
using Cake.AppCenter;
using Cake.Common;
using Cake.Frosting;


namespace ShinyBuild.Tasks.iOS
{
    [TaskName("iOSSample")]
    [IsDependentOn(typeof(BuildTask))]
    public sealed class DeployTask : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
            => context.IsRunningInCI && !context.IsPullRequest && context.IsRunningOnMacOs();


        public override void Run(BuildContext context)
        {
            context.AppCenterDistributeGroupsPublish(new AppCenterDistributeGroupsPublishSettings
            {
            });
        }
    }
}
