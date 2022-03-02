namespace Shiny
{
    public struct AndroidPermission
    {
        public AndroidPermission(string permission, int? minSdkVersion, int? maxSdkVersion)
        {
            this.Permission = permission;
            this.MinSdkVersion = minSdkVersion;
            this.MaxSdkVersion = maxSdkVersion;
        }

        public string Permission { get; }
        public int? MinSdkVersion { get; }
        public int? MaxSdkVersion { get; }
    }
}
