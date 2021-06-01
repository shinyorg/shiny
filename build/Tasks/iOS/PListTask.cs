using System;
using System.Collections.Generic;
using Cake.Common.IO;
using Cake.Frosting;
using Cake.Plist;


namespace ShinyBuild.Tasks.iOS
{
    public sealed class PListTask : FrostingTask<BuildContext>
    {
        const string InfoPlist = "samples/Samples.iOS/Info.plist";
        const string EntitlementsPlist = "samples/Samples.iOS/Entitlements.plist";


        public override void Run(BuildContext context)
        {
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
}
