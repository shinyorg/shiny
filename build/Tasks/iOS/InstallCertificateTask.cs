using System;
using Cake.Common.IO;
using Cake.Frosting;


namespace ShinyBuild.Tasks.iOS
{
    //[TaskName("ioscertificate")]
    public sealed class InstallCertificateTask : FrostingTask<BuildContext>
    {
        //public override bool ShouldRun(BuildContext context)
            //=> context.IsRunningInCI && !context.IsWindows;


        public override void Run(BuildContext context)
        {
            //security import Certificate.p12 -k ~/Library/Keychains/login.keychain -P password -T /usr/bin/codesign
            var exe = context.File("security");

            var process = context.Execute(exe, "unlock-keychain -u");
            process.WaitForExit();
            //if (process.GetStandardError)

            //process = context.Execute(exe, "import Certificate.p12 -k ~/Library/Keychains/login.keychain -P password -T /usr/bin/codesign");
            //process.WaitForExit();
        }
    }
}