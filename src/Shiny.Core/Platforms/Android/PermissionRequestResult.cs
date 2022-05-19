using System.Linq;
using NativePerm = Android.Content.PM.Permission;

namespace Shiny;


public record PermissionRequestResult(
    int RequestCode, 
    string[] Permissions, 
    NativePerm[] GrantResults
)
{
    public bool IsSuccess() => this.GrantResults.All(x => x == NativePerm.Granted);
    public int Length => this.Permissions?.Length ?? 0;
    public (string, NativePerm) this[int index] => (this.Permissions[index], this.GrantResults[index]);


    public bool IsGranted(string permission)
    {
        var index = this.Permissions.ToList().IndexOf(permission);
        if (index == -1)
            return false;

        return this.GrantResults[index] == NativePerm.Granted;
    }
}