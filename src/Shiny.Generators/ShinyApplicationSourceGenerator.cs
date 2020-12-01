using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    public abstract class ShinyApplicationSourceGenerator : ISourceGenerator
    {
        const string GENERATED_STARTUP_TYPE_NAME = "AppShinyStartup";
        readonly string osApplicationTypeName;

        protected ShinyApplicationSourceGenerator(string osApplicationTypeName) => this.osApplicationTypeName = osApplicationTypeName;
        protected IShinyContext? Context { get; private set; }
        protected void AddSource(string sourceText, string? fileName = null) => this.Context.Context.Source(sourceText, fileName);
        internal ShinyApplicationValues ShinyConfig { get; private set; }


        public virtual void Execute(GeneratorExecutionContext context)
        {
            var shinyAppAttributeData = context.GetCurrentAssemblyAttribute(Constants.ShinyApplicationAttributeTypeName);
            if (shinyAppAttributeData == null)
                return;

            var appType = context.Compilation.GetTypeByMetadataName(this.osApplicationTypeName);
            if (appType == null)
                return;

            this.ShinyConfig = new ShinyApplicationValues(shinyAppAttributeData);
            this.Context = new ShinyContext(context);

            // TODO: search current assembly for one first
            if (String.IsNullOrWhiteSpace(this.ShinyConfig.ShinyStartupTypeName))
            {
                this.GenerateStartup();
                this.ShinyConfig.ShinyStartupTypeName = GENERATED_STARTUP_TYPE_NAME;
            }

            var appClasses = context
                .Compilation
                .Assembly
                .GetAllTypeSymbols()
                .Where(x => x.Inherits(appType))
                .ToList();

            this.Process(appClasses);
        }


        public virtual void Initialize(GeneratorInitializationContext context) { }


        protected abstract void Process(IEnumerable<INamedTypeSymbol> osAppTypeSymbols);


        IndentedStringBuilder builder;
        protected void GenerateStartup()
        {
            var nameSpace = this.Context.Context.Compilation.Assembly.GlobalNamespace.Name;
            this.builder = new IndentedStringBuilder();
            this.builder.AppendNamespaces("Shiny");

            using (this.builder.BlockInvariant("namespace " + nameSpace))
            {
                using (this.builder.BlockInvariant("public class " + GENERATED_STARTUP_TYPE_NAME))
                {
                    using (this.builder.BlockInvariant("public override void ConfigureServices(IServiceCollection services)"))
                    {
                        //if (existing?.HasMethod("CustomConfigureServices") ?? false)
                        //    this.builder.AppendLineInvariant("this.CustomConfigureServices(services);");

                        this.RegisterIf("Shiny.BluetoothLE.Hosting.IBleHostingManager", "services.UseBleHosting();");
                        this.RegisterIf("Shiny.Nfc.INfcManager", "services.UseNfc();");
                        this.RegisterIf("Shiny.Sensors.IAccelerometer", "services.UseAllSensors();");
                        this.RegisterIf("Shiny.SpeechRecognition.ISpeechRecognizer", "services.UseSpeechRecognition();");
                        this.RegisterIf("Shiny.Locations.IGpsManager", "services.UseMotionActivity();");

                        this.RegisterAllDelegate("Shiny.Locations.IGpsDelegate", "services.UseGps", false);
                        this.RegisterAllDelegate("Shiny.Locations.IGeofenceDelegate", "services.UseGeofencing", true);
                        this.RegisterAllDelegate("Shiny.BluetoothLE.IBleDelegate", "services.UseBleClient", false);
                        this.RegisterAllDelegate("Shiny.Notifications.INotificationDelegate", "services.UseNotifications", false);
                        this.RegisterAllDelegate("Shiny.Net.Http.IHttpTransferDelegate", "services.UseHttpTransfers", true);
                        this.RegisterAllDelegate("Shiny.Beacons.IBeaconMonitorDelegate", "services.UseBeaconRanging", true);
                        this.RegisterAllDelegate("Shiny.Locations.Sync.IGeofenceSyncDelegate", "services.UseGeofencingSync", true);
                        this.RegisterAllDelegate("Shiny.Locations.Sync.IGpsSyncDelegate", "services.UseGpsSync", true);
                        this.RegisterAllDelegate("Shiny.TripTracker.ITripTrackerDelegate", "services.UseTripTracker", true);
                        this.RegisterAllDelegate("Shiny.DataSync.IDataSyncDelegate", "services.UseDataSync", true);

                        this.RegisterPush();

                        if (this.ShinyConfig.ExcludeJobs)
                            this.RegisterJobs();

                        if (this.ShinyConfig.ExcludeModules)
                            this.RegisterModules();

                        if (this.ShinyConfig.ExcludeStartupTasks)
                            this.RegisterStartupTasks();

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
            this.Context.Context.Source(this.builder.ToString(), GENERATED_STARTUP_TYPE_NAME);
        }


        void RegisterPush()
        {
            var hasAzurePush = this.Context.Context.Compilation.ReferencedAssemblyNames.Any(x => x.Equals("Shiny.Push.AzureNotificationHubs"));
            if (hasAzurePush)
            {
                //var rootNs = this.Context.Context.GetRootNamespace();
                //this.Log.Warn($"Shiny.Push.AzureNotificationHubs cannot be auto-registered due to required configuration parameters.  Make sure to create a `public partial class AppShinyStartup : Shiny.ShinyStartup` in the namespace `{rootNs}` in this project with the rootnamespace and add `void CustomConfigureServices(IServiceCollection services)` to register it");
            }
            else
            {
                // azure must be manually registered
                var hasFirebasePush = this.Context.Context.Compilation.ReferencedAssemblyNames.Any(x => x.Equals("Shiny.Push.FirebaseMessaging"));
                var hasNativePush = this.Context.Context.Compilation.ReferencedAssemblyNames.Any(x => x.Equals("Shiny.Push"));

                if (hasFirebasePush)
                {
                    this.RegisterAllDelegate("Shiny.Push.IPushDelegate", "services.UseFirebaseMessaging", false);
                }
                else if (hasNativePush)
                {
                    this.RegisterAllDelegate("Shiny.Push.IPushDelegate", "services.UsePush", false);
                }
            }
        }


        bool RegisterIf(string typeNameExists, string registerString)
        {
            var symbol = this.Context.Context.Compilation.GetTypeByMetadataName(typeNameExists);
            if (symbol != null)
            {
                //this.Log.Info("Registering in Shiny Startup - " + registerString);
                this.builder.AppendLineInvariant(registerString);
                return true;
            }
            return false;
        }


            bool RegisterAllDelegate(string delegateTypeName, string registerStatement, bool oneDelegateRequiredToInstall)
            {
                var symbol = this.Context.Context.Compilation.GetTypeByMetadataName(delegateTypeName);
                if (symbol == null)
                    return false;

                var impls = this.Context
                    .Context
                    .Compilation
                    .Assembly
                    .GetAllTypeSymbols()
                    .Where(x => x.Inherits(symbol))
                    .ToList();


                if (!impls.Any() && oneDelegateRequiredToInstall)
                    return false;

                if (oneDelegateRequiredToInstall)
                    registerStatement += $"<{impls.First().ToDisplayString()}>";

                registerStatement += "();";
                this.builder.AppendLineInvariant(registerStatement);

                if (impls.Count > 1)
                {
                    var startIndex = oneDelegateRequiredToInstall ? 1 : 0;
                    for (var i = startIndex; i < impls.Count; i++)
                    {
                        var impl = impls[i];
                        this.builder.AppendLineInvariant($"services.AddSingleton<{delegateTypeName}, {impl.ToDisplayString()}>();");
                    }
                }
                return true;
            }


        void RegisterJobs() => this.RegisterTypes(
            "Shiny.Jobs.IJob",
            false,
            x => this.builder.AppendLineInvariant($"services.RegisterJob(typeof({x.ToDisplayString()}));")
        );


        void RegisterStartupTasks() => this.RegisterTypes(
            "Shiny.IShinyStartupTask",
            false,
            x => this.builder.AppendLineInvariant($"services.AddSingleton<{x.ToDisplayString()}>();")
        );


        void RegisterModules() => this.RegisterTypes(
            "Shiny.ShinyModule",
            true,
            x => this.builder.AppendLineInvariant($"services.RegisterModule<{x.ToDisplayString()}>();")
        );


        void RegisterTypes(string searchType, bool inherits, Action<INamedTypeSymbol> action)
        {
            var symbol = this.Context.Context.Compilation.GetTypeByMetadataName("Shiny.Jobs.IJob");
            var jobTypes = this
                .Context
                .Context
                .Compilation
                .Assembly
                .GetAllTypeSymbols()
                // Where not system
                .Where(x => inherits
                    ? x.Inherits(symbol)
                    : x.Implements(symbol)
                );

            foreach (var type in jobTypes)
                this.builder.AppendLineInvariant($"services.RegisterJob(typeof({type.ToDisplayString()}));");
        }
    }
}
