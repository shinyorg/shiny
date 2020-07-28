using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.RoslynHelpers;


namespace Shiny.Generators.Tasks
{
    public class AutoStartupTask : ShinySourceGeneratorTask
    {
        readonly IndentedStringBuilder builder = new IndentedStringBuilder();


        public override void Execute()
        {
            if (!this.Context.HasAssemblyAttribute(typeof(GenerateStartupAttribute).FullName))
            {
                this.Log.Info("Assembly is not setup to auto-build the Shiny Startup");
                return;
            }
            this.Log.Info("Generating Shiny Startup");

            this.ShinyContext.IsStartupGenerated = true;
            var nameSpace = this.ShinyContext.GetRootNamespace();
            var existing = this.Context.Compilation.GetTypeByMetadataName($"{nameSpace}.AppShinyStartup");

            this.builder.AppendNamespaces("Microsoft.Extensions.DependencyInjection");
            this.builder.CreateClass(
                () =>
                {
                    using (this.builder.BlockInvariant("public override void ConfigureServices(IServiceCollection services)"))
                    {
                        if (existing?.HasMethod("CustomConfigureServices") ?? false)
                            this.builder.AppendLineInvariant("this.CustomConfigureServices(services);");

                        this.RegisterIf("Shiny.BluetoothLE.Hosting.IBleHostingManager", "services.UseBleHosting();");
                        this.RegisterIf("Shiny.Nfc.INfcManager", "services.UseNfc();");
                        this.RegisterIf("Shiny.Sensors.IAccelerometer", "services.UseAllSensors();");
                        this.RegisterIf("Shiny.SpeechRecognition.ISpeechRecognizer", "services.UseSpeechRecognition();");
                        this.RegisterIf("Shiny.Locations.IGpsManager", "services.UseMotionActivity();");

                        this.RegisterAllDelegate("Shiny.Locations.IGpsDelegate", "services.UseGps", false);
                        this.RegisterAllDelegate("Shiny.Locations.IGeofenceDelegate", "services.UseGeofencing", true);
                        this.RegisterAllDelegate("Shiny.BluetoothLE.IBleDelegate", "services.UseBleClient", false);
                        this.RegisterAllDelegate("Shiny.Notifications.INotificationDelegate", "services.UseNotifications", false);
                        this.RegisterAllDelegate("Shiny.MediaSync.IMediaSyncDelegate", "services.UseMediaSync", false);
                        this.RegisterAllDelegate("Shiny.Net.Http.IHttpTransferDelegate", "services.UseHttpTransfers", true);
                        this.RegisterAllDelegate("Shiny.Beacons.IBeaconMonitorDelegate", "services.UseBeaconRanging", true);
                        this.RegisterAllDelegate("Shiny.Locations.Sync.IGeofenceSyncDelegate", "services.UseGeofencingSync", true);
                        this.RegisterAllDelegate("Shiny.Locations.Sync.IGpsSyncDelegate", "services.UseGpsSync", true);
                        this.RegisterAllDelegate("Shiny.TripTracker.ITripTrackerDelegate", "services.UseTripTracker", true);

                        this.RegisterJobs();
                        this.RegisterStartupTasks();
                        this.RegisterModules();
                        this.RegisterInjects();
                    }

                    if (this.Context.HasXamarinForms())
                    {
                        using (this.builder.BlockInvariant("public override void ConfigureApp(IServiceProvider provider)"))
                        {
                            this.builder.AppendFormatInvariant("global::Xamarin.Forms.Internals.DependencyResolver.ResolveUsing(t => provider.GetService(t));");
                            this.builder.AppendLine();
                        }
                    }
                },
                nameSpace,
                "AppShinyStartup",
                "Shiny.ShinyStartup"
            );

            this.Context.AddCompilationUnit("AppShinyStartup", this.builder.ToString());
        }


        void RegisterIf(string typeNameExists, string registerString)
        {
            var symbol = this.Context.Compilation.GetTypeByMetadataName(typeNameExists);
            if (symbol != null)
            {
                this.Log.Info("Registering in Shiny Startup - " + registerString);
                this.builder.AppendLineInvariant(registerString);
            }
        }


        void RegisterAllDelegate(string delegateTypeName, string registerStatement, bool oneDelegateRequiredToInstall)
        {
            var symbol = this.Context.Compilation.GetTypeByMetadataName(delegateTypeName);
            if (symbol == null)
                return;

            var impls = this
                .Context
                .GetAllImplementationsOfType(delegateTypeName)
                .WhereNotSystem()
                .ToArray();

            if (!impls.Any() && oneDelegateRequiredToInstall)
                return;

            if (oneDelegateRequiredToInstall)
                registerStatement += $"<{impls.First().ToDisplayString()}>";

            registerStatement += "();";
            this.builder.AppendLineInvariant(registerStatement);

            if (impls.Length > 1)
            {
                var startIndex = oneDelegateRequiredToInstall ? 1 : 0;
                for (var i = startIndex; i < impls.Length; i++)
                {
                    var impl = impls[i];
                    this.builder.AppendLineInvariant($"services.AddSingleton<{delegateTypeName}, {impl.ToDisplayString()}>();");
                }
            }
        }


        void RegisterInjects()
        {
            var attribute = this.Context.Compilation.GetTypeByMetadataName("Shiny.Generators.ShinyInjectAttribute");
            var injects = this.Context.Compilation.Assembly.GetAllAttributes().Where(x => x.AttributeClass.Equals(attribute));

            foreach (var inject in injects)
            {
                var type1 = (INamedTypeSymbol)inject.ConstructorArguments[0].Value;
                var type2 = (INamedTypeSymbol)inject.ConstructorArguments[1].Value;
                this.builder.AppendLineInvariant($"services.AddSingleton<{type1.ToDisplayString()}, {type2.ToDisplayString()}>();");
            }
        }


        void RegisterJobs()
        {
            var jobTypes = this
                .Context
                .GetAllImplementationsOfType("Shiny.Jobs.IJob")
                .WhereNotSystem();

            foreach (var type in jobTypes)
                this.builder.AppendLineInvariant($"services.RegisterJob(typeof({type.ToDisplayString()}));");
        }


        void RegisterStartupTasks()
        {
            var types = this
                .Context
                .GetAllImplementationsOfType("Shiny.IShinyStartupTask")
                .WhereNotSystem()
                .Where(x => x.AllInterfaces.Length == 1);

            foreach (var task in types)
                this.builder.AppendLineInvariant($"services.AddSingleton<Shiny.IShinyStartupTask, {task.ToDisplayString()}>();");
        }


        void RegisterModules()
        {
            var types = this
                .Context
                .GetAllImplementationsOfType("Shiny.IShinyModule")
                .WhereNotSystem();

            foreach (var type in types)
                this.builder.AppendLineInvariant($"services.RegisterModule<{type.ToDisplayString()}>();");
        }
    }
}
