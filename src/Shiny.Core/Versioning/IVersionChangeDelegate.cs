using System;
using System.Threading.Tasks;


namespace Shiny.Versioning
{
    public abstract class VersionChangeDelegate
    {
        public virtual Task OnAppVersionChanged(string? oldVersion, string newVersion) => Task.CompletedTask;
        public virtual Task OnShinyLibraryChanged(string? oldVersion, string newVersion) => Task.CompletedTask;
        public virtual Task OnOperatingSystemVersionChanged(string? oldVersion, string newVersion) => Task.CompletedTask;
    }


    public interface IVersionChangeDelegate
    {
        Task OnAppVersionChanged(string? oldVersion, string newVersion);
        Task OnShinyLibraryChanged(string? oldVersion, string newVersion);
        Task OnOperatingSystemVersionChanged(string? oldVersion, string newVersion);
    }
}
