using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Shiny.Build;


public class AndroidPermissionsTask : Task
{
    [Required]
    public string IntermediateOutputPath { get; set; } = null!;

    [Required]
    public string ManifestOutputFile { get; set; } = null!;

    [Required]
    public ITaskItem[] Permissions { get; set; } = null!;


    public override bool Execute()
    {
        var distinctPerms = this.Permissions
            .Select(x => x.ItemSpec)
            .Where(x => !String.IsNullOrWhiteSpace(x))
            .Distinct()
            .ToList();


        var manifest = new List<string>();

        foreach (var perm in distinctPerms)
        {
            Log.LogMessage("Checking Permission: " + perm);

            var cfg = Configuration.GetPermissionByName(perm);
            if (cfg == null)
            {
                Log.LogError("{0} is an invalid permission", perm);
            }
            else if (cfg.AndroidManifest != null)
            {
                foreach (var entry in cfg.AndroidManifest)
                {
                    if (!manifest.Any(x => x.Equals(entry, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        manifest.Add(entry);
                    }
                }
            }
        }

        if (!Log.HasLoggedErrors)
        {
            Directory.CreateDirectory(this.IntermediateOutputPath);
            var filePath = Path.Combine(this.IntermediateOutputPath, this.ManifestOutputFile);

            using var file = File.CreateText(filePath);
            foreach (var entry in manifest)
            {
                var e = entry.ToCamelCase();
                file.WriteLine($"[assembly: Android.App.UsesPermission(Android.Manifest.Permission.{e})]");
            }
            //file.WriteLine("<?xml version = \"1.0\" encoding = \"utf-8\" ?>");
            //file.WriteLine("<manifest xmlns:android = \"http://schemas.android.com/apk/res/android\" package=\"org.shiny.buildtasksample\">");

            //foreach (var entry in manifest)
            //{
            //    var e = entry.ToUpper();
            //    file.WriteLine($"    <uses-permission android:name=\"android.permission.{e}\" />");
            //}
            //file.WriteLine("</manifest>");
            Log.LogMessage("Generated Shiny Android Manifest: " + filePath);
        }
        return !Log.HasLoggedErrors;
    }



}