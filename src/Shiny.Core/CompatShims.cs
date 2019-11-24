using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Shiny.IO;
using Shiny.Jobs;
using Shiny.Net;
using Shiny.Power;
using Shiny.Settings;


namespace Shiny
{
    public static class CrossJobManager
    {
        static IJobManager Current { get; } = ShinyHost.Resolve<IJobManager>();

        public static bool IsRunning => Current.IsRunning;
        public static event EventHandler<JobInfo> JobStarted
        {
            add { Current.JobStarted += value; }
            remove { Current.JobStarted -= value; }
        }
        public static event EventHandler<JobRunResult> JobFinished
        {
            add => Current.JobFinished += value;
            remove => Current.JobFinished -= value;
        }
        public static Task Cancel(string jobName) => Current.Cancel(jobName);
        public static Task CancelAll() => Current.CancelAll();
        public static Task<JobInfo?> GetJob(string jobIdentifier) => Current.GetJob(jobIdentifier);
        public static Task<IEnumerable<JobInfo>> GetJobs() => Current.GetJobs();
        public static Task<AccessState> RequestAccess() => Current.RequestAccess();
        public static Task<JobRunResult> Run(string jobIdentifier, CancellationToken cancelToken = default) => Current.Run(jobIdentifier, cancelToken);
        public static Task<IEnumerable<JobRunResult>> RunAll(CancellationToken cancelToken = default) => Current.RunAll(cancelToken);
        public static void RunTask(string taskName, Func<CancellationToken, Task> task) => Current.RunTask(taskName, task);
        public static Task Schedule(JobInfo jobInfo) => Current.Schedule(jobInfo);
    }


    public static class CrossFileSystem
    {
        static IFileSystem Current { get; } = ShinyHost.Resolve<IFileSystem>();
        public static DirectoryInfo AppData { get => Current.AppData; set => Current.AppData = value; }
        public static DirectoryInfo Cache { get => Current.Cache; set => Current.Cache = value; }
        public static DirectoryInfo Public { get => Current.Public; set => Current.Public = value; }
    }


    public static class CrossPower
    {
        static IPowerManager Current { get; } = ShinyHost.Resolve<IPowerManager>();
        public static PowerState Status => Current.Status;
        public static int BatteryLevel => Current.BatteryLevel;
        public static event PropertyChangedEventHandler PropertyChanged
        {
            add => Current.PropertyChanged += value;
            remove => Current.PropertyChanged -= value;
        }
    }


    public static class CrossConnectivity
    {
        static IConnectivity Current { get; } = ShinyHost.Resolve<IConnectivity>();
        public static NetworkReach Reach => Current.Reach;
        public static NetworkAccess Access => Current.Access;
        public static event PropertyChangedEventHandler PropertyChanged
        {
            add => Current.PropertyChanged += value;
            remove => Current.PropertyChanged -= value;
        }
    }


    public static class CrossSettings
    {
        static ISettings Current { get; } = ShinyHost.Resolve<ISettings>();

        public static List<string> KeysNotToClear => Current.KeysNotToClear;
        public static IReadOnlyDictionary<string, string> List => Current.List;
        public static event EventHandler<SettingChangeEventArgs> Changed
        {
            add => Current.Changed += value;
            remove => Current.Changed -= value;
        }
        public static T Bind<T>() where T : INotifyPropertyChanged, new() => Current.Bind<T>();
        public static void Bind(INotifyPropertyChanged obj) => Current.Bind(obj);
        public static void Clear() => Current.Clear();
        public static bool Contains(string key) => Current.Contains(key);
        public static T Get<T>(string key, T defaultValue = default) => Current.Get(key, defaultValue);
        public static T GetRequired<T>(string key) => Current.GetRequired<T>(key);
        public static object? GetValue(Type type, string key, object? defaultValue = null) => Current.GetValue(type, key, defaultValue);
        public static bool Remove(string key) => Current.Remove(key);
        public static void Set<T>(string key, T value) => Current.Set(key, value);
        public static bool SetDefault<T>(string key, T value) => Current.SetDefault(key, value);
        public static void SetValue(string key, object value) => Current.SetValue(key, value);
        public static void UnBind(INotifyPropertyChanged obj) => Current.UnBind(obj);
    }


    public static class CrossEnvironment
    {
        static IEnvironment Current { get; } = ShinyHost.Resolve<IEnvironment>();

        public static string AppIdentifier => Current.AppIdentifier;
        public static string AppVersion => Current.AppVersion;
        public static string AppBuild => Current.AppBuild;
        public static string MachineName => Current.MachineName;
        public static string OperatingSystem => Current.OperatingSystem;
        public static string OperatingSystemVersion => Current.OperatingSystemVersion;
        public static string Manufacturer => Current.Manufacturer;
        public static string Model => Current.Model;
    }
}
