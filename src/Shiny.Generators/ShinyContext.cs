using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    public interface IShinyContext
    {
        GeneratorExecutionContext Context { get; }
        INamedTypeSymbol? GetShinyType(string fullyQualifiedMetadataName);
        void GenerateAndAddShinyStartup();
        //string? GetShinyStartupClassFullName();
        //string? GetXamFormsAppClassFullName();
    }


    public class ShinyContext : IShinyContext
    {
        readonly Lazy<IAssemblySymbol[]> shinyAssemblies;


        public ShinyContext(GeneratorExecutionContext context)
        {
            this.Context = context;
            this.shinyAssemblies = new Lazy<IAssemblySymbol[]>(() => context
                .Compilation
                .References
                .Where(x =>
                    x.Display != null &&
                    x.Properties.Kind == MetadataImageKind.Assembly &&
                    Regex.IsMatch(x.Display, "Shiny.(.*).dll")
                )
                .Select(context.Compilation.GetAssemblyOrModuleSymbol)
                .OfType<IAssemblySymbol>()
                .ToArray()
            );
        }


        public GeneratorExecutionContext Context { get; private set; }


        public INamedTypeSymbol? GetShinyType(string fullyQualifiedMetadataName) => this.shinyAssemblies
            .Value
            .Select(x => x.GetTypeByMetadataName(fullyQualifiedMetadataName))
            .FirstOrDefault();


        public void GenerateAndAddShinyStartup()
        {
            var startupTypeName = "TestShinyStartup"; // TODO
            var nameSpace = this.Context.Compilation.Assembly.GlobalNamespace.Name;
            var hasPush = this.shinyAssemblies.Value.Any(x => x.Name.StartsWith("Shiny.Push"));

            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces("Shiny");

            using (builder.BlockInvariant("namespace " + nameSpace))
            {
                using (builder.BlockInvariant("public class " + startupTypeName))
                {
                    //using (this.builder.BlockInvariant("public override void ConfigureServices(IServiceCollection services)"))
                    //{
                    //    if (existing?.HasMethod("CustomConfigureServices") ?? false)
                    //        this.builder.AppendLineInvariant("this.CustomConfigureServices(services);");

                    //    this.RegisterIf("Shiny.BluetoothLE.Hosting.IBleHostingManager", "services.UseBleHosting();");
                    //    this.RegisterIf("Shiny.Nfc.INfcManager", "services.UseNfc();");
                    //    this.RegisterIf("Shiny.Sensors.IAccelerometer", "services.UseAllSensors();");
                    //    this.RegisterIf("Shiny.SpeechRecognition.ISpeechRecognizer", "services.UseSpeechRecognition();");
                    //    this.RegisterIf("Shiny.Locations.IGpsManager", "services.UseMotionActivity();");

                    //    this.RegisterAllDelegate("Shiny.Locations.IGpsDelegate", "services.UseGps", false);
                    //    this.RegisterAllDelegate("Shiny.Locations.IGeofenceDelegate", "services.UseGeofencing", true);
                    //    this.RegisterAllDelegate("Shiny.BluetoothLE.IBleDelegate", "services.UseBleClient", false);
                    //    this.RegisterAllDelegate("Shiny.Notifications.INotificationDelegate", "services.UseNotifications", false);
                    //    this.RegisterAllDelegate("Shiny.MediaSync.IMediaSyncDelegate", "services.UseMediaSync", false);
                    //    this.RegisterAllDelegate("Shiny.Net.Http.IHttpTransferDelegate", "services.UseHttpTransfers", true);
                    //    this.RegisterAllDelegate("Shiny.Beacons.IBeaconMonitorDelegate", "services.UseBeaconRanging", true);
                    //    this.RegisterAllDelegate("Shiny.Locations.Sync.IGeofenceSyncDelegate", "services.UseGeofencingSync", true);
                    //    this.RegisterAllDelegate("Shiny.Locations.Sync.IGpsSyncDelegate", "services.UseGpsSync", true);
                    //    this.RegisterAllDelegate("Shiny.TripTracker.ITripTrackerDelegate", "services.UseTripTracker", true);
                    //    this.RegisterAllDelegate("Shiny.DataSync.IDataSyncDelegate", "services.UseDataSync", true);

                    //    this.RegisterPush();
                    //    this.RegisterJobs();
                    //    this.RegisterStartupTasks();
                    //    this.RegisterModules();
                    //    this.RegisterInjects();
                    //}

                    //if (this.Context.HasXamarinForms())
                    //{
                    //    using (this.builder.BlockInvariant("public override void ConfigureApp(IServiceProvider provider)"))
                    //    {
                    //        this.builder.AppendFormatInvariant("global::Xamarin.Forms.Internals.DependencyResolver.ResolveUsing(t => provider.GetService(t));");
                    //        this.builder.AppendLine();
                    //    }
                    //}
                }
            }
        }


        //void RegisterPush()
        //{
        //    var hasAzurePush = this.Context.Compilation.ReferencedAssemblyNames.Any(x => x.Equals("Shiny.Push.AzureNotificationHubs"));
        //    if (hasAzurePush)
        //    {
        //        var rootNs = this.ShinyContext.GetRootNamespace();
        //        this.Log.Warn($"Shiny.Push.AzureNotificationHubs cannot be auto-registered due to required configuration parameters.  Make sure to create a `public partial class AppShinyStartup : Shiny.ShinyStartup` in the namespace `{rootNs}` in this project with the rootnamespace and add `void CustomConfigureServices(IServiceCollection services)` to register it");
        //    }
        //    else
        //    {
        //        // azure must be manually registered
        //        var hasFirebasePush = this.Context.Compilation.ReferencedAssemblyNames.Any(x => x.Equals("Shiny.Push.FirebaseMessaging"));
        //        var hasNativePush = this.Context.Compilation.ReferencedAssemblyNames.Any(x => x.Equals("Shiny.Push"));

        //        if (hasFirebasePush)
        //        {
        //            this.RegisterAllDelegate("Shiny.Push.IPushDelegate", "services.UseFirebaseMessaging", false);
        //        }
        //        else if (hasNativePush)
        //        {
        //            this.RegisterAllDelegate("Shiny.Push.IPushDelegate", "services.UsePush", false);
        //        }
        //    }
        //}


        //bool RegisterIf(string typeNameExists, string registerString)
        //{
        //    var symbol = this.Context.Compilation.GetTypeByMetadataName(typeNameExists);
        //    if (symbol != null)
        //    {
        //        this.Log.Info("Registering in Shiny Startup - " + registerString);
        //        this.builder.AppendLineInvariant(registerString);
        //        return true;
        //    }
        //    return false;
        //}


        //bool RegisterAllDelegate(string delegateTypeName, string registerStatement, bool oneDelegateRequiredToInstall)
        //{
        //    var symbol = this.Context.Compilation.GetTypeByMetadataName(delegateTypeName);
        //    if (symbol == null)
        //        return false;

        //    var impls = this
        //        .Context
        //        .GetAllImplementationsOfType(delegateTypeName)
        //        .WhereNotSystem()
        //        .ToArray();

        //    if (!impls.Any() && oneDelegateRequiredToInstall)
        //        return false;

        //    if (oneDelegateRequiredToInstall)
        //        registerStatement += $"<{impls.First().ToDisplayString()}>";

        //    registerStatement += "();";
        //    this.builder.AppendLineInvariant(registerStatement);

        //    if (impls.Length > 1)
        //    {
        //        var startIndex = oneDelegateRequiredToInstall ? 1 : 0;
        //        for (var i = startIndex; i < impls.Length; i++)
        //        {
        //            var impl = impls[i];
        //            this.builder.AppendLineInvariant($"services.AddSingleton<{delegateTypeName}, {impl.ToDisplayString()}>();");
        //        }
        //    }
        //    return true;
        //}


        //void RegisterJobs()
        //{
        //    var jobTypes = this
        //        .Context
        //        .GetAllImplementationsOfType("Shiny.Jobs.IJob")
        //        .WhereNotSystem();

        //    foreach (var type in jobTypes)
        //        this.builder.AppendLineInvariant($"services.RegisterJob(typeof({type.ToDisplayString()}));");
        //}


        //void RegisterStartupTasks()
        //{
        //    var types = this
        //        .Context
        //        .GetAllImplementationsOfType("Shiny.IShinyStartupTask")
        //        .WhereNotSystem()
        //        .Where(x => x.AllInterfaces.Length == 1);

        //    foreach (var task in types)
        //        this.builder.AppendLineInvariant($"services.AddSingleton<Shiny.IShinyStartupTask, {task.ToDisplayString()}>();");
        //}


        //void RegisterModules()
        //{
        //    var types = this
        //        .Context
        //        .GetAllImplementationsOfType("Shiny.IShinyModule")
        //        .WhereNotSystem();

        //    foreach (var type in types)
        //        this.builder.AppendLineInvariant($"services.RegisterModule<{type.ToDisplayString()}>();");
        //}
    }
}
