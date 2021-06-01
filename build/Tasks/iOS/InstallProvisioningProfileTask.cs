using System;
using Cake.Common;
using Cake.Frosting;


namespace ShinyBuild.Tasks.iOS
{
    public sealed class InstallProvisioningProfileTask : FrostingTask<BuildContext>
    {
        public override void Run(BuildContext context)
        {
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
        }
    }
}
