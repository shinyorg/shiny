using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shiny.Build;


public class ApplePermissionsTask : Task
{
    const string plistHeader =
@"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE plist PUBLIC ""-//Apple//DTD PLIST 1.0//EN"" ""http://www.apple.com/DTDs/PropertyList-1.0.dtd"">
<plist version=""1.0"">
<dict>";
    const string plistFooter = @"
</dict>
</plist>";


    [Required]
    public string IntermediateOutputPath { get; set; } = null!;

    public string InfoPlistOutputFile { get; set; } = "ShinyPartialInfo.plist";
    //public string EntitlementsPlistOutputFile { get; set; }

    [Required]
    public ITaskItem[] Permissions { get; set; } = null!;


    public override bool Execute()
    {
        var distinctPerms = this.Permissions
            .Select(x => x.ItemSpec.ToLower())
            .Where(x => !String.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();

        var bgModes = new List<string>();
        var entitlements = new List<string>();
        var infoKeys = new List<string>();

        foreach (var perm in distinctPerms)
        {
            Log.LogMessage("Checking Permission Details for " + perm);

            var cfg = Configuration.GetPermissionByName(perm);
            if (cfg == null)
            {
                Log.LogError("{0} is an invalid permission", perm);
            }
            else if (cfg.Ios != null)
            {
                AppendUnique(infoKeys, cfg.Ios.Info);
                AppendUnique(bgModes, cfg.Ios.BackgroundModes);
                AppendUnique(entitlements, cfg.Ios.Entitlements);
            }
        }

        if (!Log.HasLoggedErrors)
        {
            Directory.CreateDirectory(this.IntermediateOutputPath);
            var plistFilename = Path.Combine(this.IntermediateOutputPath, this.InfoPlistOutputFile);

            using var file = File.CreateText(plistFilename);
            file.WriteLine(plistHeader);

            foreach (var key in infoKeys)
            {
                Log.LogMessage("Writing Apple Info.plist key: " + key);
                file.WriteLine($"    <key>{key}</key>");
                file.WriteLine($"    <string>TODO</string>");
            }

            if (bgModes.Count > 0)
            {

                file.WriteLine("    <key>UIBackgroundModes</key>");
                file.WriteLine("    <array>");
                foreach (var bg in bgModes)
                {
                    Log.LogMessage("Writing Apple Background Mode: " + bg);

                    file.WriteLine($"        <string>{bg}</string>");
                }
                file.WriteLine("    </array>");
            }
            file.WriteLine(plistFooter);

            Log.LogMessage("Generated Shiny Apple Manifest: " + plistFilename);
        }
        return !Log.HasLoggedErrors;
    }


    static void AppendUnique(List<string> list, string[]? values)
    {
        if (values == null)
            return;

        foreach (var value in values)
        {
            if (!list.Any(x => x.Equals(value, System.StringComparison.CurrentCultureIgnoreCase)))
                list.Add(value);
        }
    }
}
//Entitlements.plist
//<!DOCTYPE plist PUBLIC " -//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
//<plist version =\"1.0\">
//    <dict>
//        <key>aps-environment</key>
//        <string>development OR production</string>
//    </dict>
//</plist>