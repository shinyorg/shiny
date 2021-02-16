using System;
using System.Threading.Tasks;
using Samples.Models;
using Shiny.Versioning;


namespace Samples.Versioning
{
    public class SampleVersionChangeDelegate : IVersionChangeDelegate
    {
        readonly SampleSqliteConnection conn;
        public SampleVersionChangeDelegate(SampleSqliteConnection conn)
            => this.conn = conn;


        public Task OnAppVersionChanged(string? oldVersion, string newVersion) => this.conn.InsertAsync(new VersionChange
        {
            Area = "App",
            Old = oldVersion,
            New = newVersion
        });


        public Task OnOperatingSystemVersionChanged(string? oldVersion, string newVersion) => this.conn.InsertAsync(new VersionChange
        {
            Area = "OS",
            Old = oldVersion,
            New = newVersion
        });
    }
}
