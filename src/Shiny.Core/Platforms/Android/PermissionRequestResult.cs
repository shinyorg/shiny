using System;
using System.Linq;
using NativePerm = Android.Content.PM.Permission;


namespace Shiny
{
    public struct PermissionRequestResult
    {
        public PermissionRequestResult(int requestCode, string[] permissions, NativePerm[] grantResults)
        {
            this.RequestCode = requestCode;
            this.Permissions = permissions;
            this.GrantResults = grantResults;
        }


        public bool IsSuccess() => this.GrantResults.All(x => x == NativePerm.Granted);
        public int RequestCode { get; }
        public string[] Permissions { get; }
        public NativePerm[] GrantResults { get; }


        public int Length => this.Permissions?.Length ?? 0;
        public (string, NativePerm) this[int index] => (this.Permissions[index], this.GrantResults[index]);
    }
}
