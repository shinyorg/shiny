using System;
using System.Collections.Generic;
using Cake.AppCenter;
using Cake.Common.IO;
using Cake.Common.Tools.MSBuild;
using Cake.Frosting;
using Cake.Plist;


namespace ShinyBuild.Tasks.Samples
{
    public sealed class IosPlistTask : FrostingTask<BuildContext>
    {
        const string InfoPlist = "samples/Samples.iOS/Info.plist";
        const string EntitlementsPlist = "samples/Samples.iOS/Entitlements.plist";


//        security unlock-keychain -p<my keychain password>
//security import Certificate.p12 -k ~/Library/Keychains/login.keychain -P password -T /usr/bin/codesign
        public override void Run(BuildContext context)
        {
            if (!context.IsRunningInCI)
                return;

            var infoPath = context.File(InfoPlist);

            var info = (Dictionary<string, object>)context.DeserializePlist(infoPath);
            info["CFBundleDisplayName"] = "Shiny Sample";
            info["CFBundleIdentifier"] = "com.shiny.test";

            var version = Version.Parse((string)info["CFBundleVersion"]);
            var newVersion = new Version(version.Major, version.Minor, version.Build + 1, 0);
            info["CFBundleVersion"] = newVersion.ToString();
            context.SerializePlist(infoPath, info);

            var entitlementPath = context.File(EntitlementsPlist);
            var entitlements = context.DeserializePlist(entitlementPath);
            entitlements["aps-environment"] = "development"; // production
            context.SerializePlist(entitlementPath, info);
        }
    }


    [TaskName("IosBuild")]
    [IsDependentOn(typeof(IosPlistTask))]
    public sealed class IosBuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            context.CleanDirectory($"../samples/**/bin/{context.MsBuildConfiguration}");
            context.MSBuild("Build.sln", x => x
                .WithTarget("Clean")
                .WithTarget("Build")
                .WithProperty("Platform", "iPhone")
                .WithProperty("BuildIpa", "true")
            //    .WithProperty("OutputPath", "bin/Release/")
            );
        }
    }


    public sealed class SignIosBuildTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {

        }
    }


    [IsDependentOn(typeof(BuildIosTask))]
    [IsDependentOn(typeof(SignIosBuildTask))]
    public sealed class IosDeployTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
            if (!context.IsMainBranch || !context.IsRunningInCI)
                return;

            // TODO: deploy Android, iOS, & UWP
            context.AppCenterDistributeGroupsPublish(new AppCenterDistributeGroupsPublishSettings
            {
            });
        }
    }
}

//static void InstallProvisioningProfile(string envVarName)
//{
//    var rx = "\\<key\\>UUID\\</key\\>\\s+?\\<string\\>(?<udid>[a-zA-Z0-9\\-]+)\\</string\\>";

//    var ppEnv = System.Environment.GetEnvironmentVariable(envVarName);

//    if (string.IsNullOrEmpty(ppEnv))
//        return;

//    var ppData = System.Convert.FromBase64String(System.Environment.GetEnvironmentVariable(envVarName));
//    var pp = System.Text.Encoding.Default.GetString(ppData);

//    var udid = System.Text.RegularExpressions.Regex.Match(pp, rx)?.Groups?["udid"]?.Value;
//    System.Console.WriteLine("{0} UDID: {1}", envVarName, udid);

//    var ppDir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "Library", "MobileDevice", "Provisioning Profiles");
//    if (!System.IO.Directory.Exists(ppDir))
//        System.IO.Directory.CreateDirectory(ppDir);

//    var ppFile = System.IO.Path.Combine(ppDir, udid + ".mobileprovision");
//    System.Console.WriteLine("{0} File: {1}", envVarName, ppFile);
//    System.IO.File.WriteAllBytes(ppFile, ppData);
//}