using System;
using Cake.Frosting;


namespace ShinyBuild.Tasks.iOS
{
    public sealed class InstallCertificateTask : FrostingTask<BuildContext>
    {
        public override bool ShouldRun(BuildContext context)
            => context.IsRunningInCI && !context.IsWindows;


        public override void Run(BuildContext context)
        {
            //        security unlock-keychain -p<my keychain password>
            //security import Certificate.p12 -k ~/Library/Keychains/login.keychain -P password -T /usr/bin/codesign
            //var result = context.ProcessRunner.Start(new Cake.Core.IO.FilePath(""), new Cake.Core.IO.ProcessSettings
            //{

            //});
        }
    }
}
