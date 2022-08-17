using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Shiny.Build;


public class ValidatePermissionsTask : Task
{
    [Required]
    public ITaskItem[] Permissions { get; set; } = null!;


    public override bool Execute()
    {
        // TODO: check for dupes?
        // TODO: validate missing apple description for permissions with plist values
        foreach (var permission in this.Permissions)
        {
            var cfg = Configuration.GetPermissionByName(permission.ItemSpec);
            if (cfg == null)
                this.Log.LogError($"{permission.ItemSpec} is an invalid permission value");
        }
        return !this.Log.HasLoggedErrors;
    }
}