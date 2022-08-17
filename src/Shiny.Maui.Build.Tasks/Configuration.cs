using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Shiny.Build;


public static class Configuration
{
    static Dictionary<string, Permission>? permissions;


    public static Permission? GetPermissionByName(string name)
    {
        EnsureLoaded();
        if (!permissions!.ContainsKey(name))
            return null;

        return permissions[name];
    }


    static void EnsureLoaded()
    {
        if (permissions != null)
            return;

        using var resource = typeof(Configuration).Assembly.GetManifestResourceStream("Shiny.Build.Permissions.json");
        using var sr = new StreamReader(resource);
        var json = sr.ReadToEnd();
        var dict = JsonSerializer.Deserialize<Dictionary<string, Permission>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        permissions = new Dictionary<string, Permission>(dict, StringComparer.InvariantCultureIgnoreCase);
    }
}


public class Permission
{
    public string[]? AndroidManifest { get; set; }
    public Ios? Ios { get; set; }
}


public class Ios
{
    public string[]? Info { get; set; }
    public string[]? BackgroundModes { get; set; }
    public string[]? Entitlements { get; set; }
}
