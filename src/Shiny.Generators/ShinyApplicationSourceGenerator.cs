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
        List<INamedTypeSymbol> allSymbols;

        protected ShinyApplicationSourceGenerator(string osApplicationTypeName) => this.osApplicationTypeName = osApplicationTypeName;
        protected GeneratorExecutionContext Context { get; private set; }
        public ShinyApplicationValues ShinyConfig { get; set; }


        public virtual void Execute(GeneratorExecutionContext context)
        {
            this.Context = context;
            var shinyAppAttributeData = context.GetCurrentAssemblyAttribute(Constants.ShinyApplicationAttributeTypeName);
            if (shinyAppAttributeData == null)
                return;

            var appType = context.Compilation.GetTypeByMetadataName(this.osApplicationTypeName);
            if (appType == null)
                return;

            // allow for testing
            this.ShinyConfig ??= new ShinyApplicationValues(shinyAppAttributeData);

            if (String.IsNullOrWhiteSpace(this.ShinyConfig.ShinyStartupTypeName))
            {
                this.GenerateStartup(this.Context.Compilation.AssemblyName);
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
        void GenerateStartup(string nameSpace)
        {
            this.allSymbols = this.Context
                .GetAllAssemblies()
                .Where(x => !x.Name.StartsWith("Shiny") && !x.Name.StartsWith("Xamarin."))
                .SelectMany(x => x.GetAllTypeSymbols())
                .Where(x => !x.IsAbstract && x.IsPublic())
                .ToList();

            this.builder = new IndentedStringBuilder();
            this.builder.AppendNamespaces("Microsoft.Extensions.DependencyInjection");

            using (this.builder.BlockInvariant("namespace " + nameSpace))
            {
                using (this.builder.BlockInvariant($"public partial class {GENERATED_STARTUP_TYPE_NAME} : Shiny.IShinyStartup"))
                {
                    this.builder.AppendLine("partial void AdditionalConfigureServices(IServiceCollection services);");

                    using (this.builder.BlockInvariant("public void ConfigureServices(IServiceCollection services)"))
                    {
                        this.builder.AppendLine("this.AdditionalConfigureServices(services);");

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

                        if (!this.ShinyConfig.ExcludeJobs)
                            this.RegisterJobs();

                        if (!this.ShinyConfig.ExcludeModules)
                            this.RegisterModules();

                        if (!this.ShinyConfig.ExcludeStartupTasks)
                            this.RegisterStartupTasks();

                        if (!this.ShinyConfig.ExcludeServices)
                            this.RegisterServices();
                    }

                    using (this.builder.BlockInvariant("public void ConfigureApp(IServiceProvider provider)"))
                    {
                        var xamFormsType = this.Context.Compilation.GetTypeByMetadataName("Xamarin.Forms.Internals.DependencyResolver");
                        if (xamFormsType != null)
                        {
                            this.builder.AppendFormatInvariant("global::Xamarin.Forms.Internals.DependencyResolver.ResolveUsing(t => provider.GetService(t));");
                            this.builder.AppendLine();
                        }
                    }
                }
            }
            this.Context.Source(this.builder.ToString(), GENERATED_STARTUP_TYPE_NAME);
        }


        static readonly string[] PushCannotGenerateRegister = new []
        {
            "Shiny.Push.AzureNotificationHubs",
            "Shiny.Push.Aws",
            "Shiny.Push.OneSignal"
        };
        static readonly Dictionary<string, string> PushRegisters = new Dictionary<string, string>
        {
            { "Shiny.Push.FirebaseMessaging", "services.UseFirebaseMessaging" },
            { "Shiny.Push", "services.UsePush" }
        };

        void RegisterPush()
        {
            var cannotRegister = this.Context.Compilation.ReferencedAssemblyNames.FirstOrDefault(x => PushCannotGenerateRegister.Any(y => y.Equals(x.Name)));
            if (cannotRegister != null)
            {
                this.Context.ReportDiagnostic(Diagnostic.Create(
                    new DiagnosticDescriptor(
                        "ShinyPush",
                        $"{cannotRegister.Name} cannot be registered with auto-generation due to required configuration",
                        null,
                        "Push",
                        DiagnosticSeverity.Warning,
                        true
                    ),
                    Location.None
                ));
            }
            else
            {
                var register = this.Context.Compilation.ReferencedAssemblyNames.FirstOrDefault(x => PushRegisters.ContainsKey(x.Name));
                if (register != null)
                {
                    var registerStatement = PushRegisters[register.Name];
                    this.RegisterAllDelegate("Shiny.Push.IPushDelegate", registerStatement, true);
                }
            }
        }


        bool RegisterIf(string typeNameExists, string registerString)
        {
            var symbol = this.Context.Compilation.GetTypeByMetadataName(typeNameExists);
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
            var symbol = this.Context.Compilation.GetTypeByMetadataName(delegateTypeName);
            if (symbol == null)
                return false;

            var impls = this.allSymbols
                .Where(x => x.Implements(symbol))
                .ToList();

            // TODO: error
            if (!impls.Any())
                return false;

            registerStatement += $"<{impls.First().ToDisplayString()}>();";
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


        void RegisterServices()
        {
            foreach (var symbol in this.allSymbols)
            {
                var attrs = symbol.GetAttributes();
                var hasService = attrs.Any(x => x.AttributeClass.Name.Equals("Shiny.ShinyServiceAttribute"));
                if (hasService)
                {
                    if (!symbol.AllInterfaces.Any())
                    {
                        this.builder.AppendLineInvariant($"services.AddSingleton<{symbol.ToDisplayString()}>();");
                    }
                    else
                    {
                        foreach (var @interface in symbol.AllInterfaces)
                            this.builder.AppendLineInvariant($"services.AddSingleton<{@interface.ToDisplayString()}, {symbol.ToDisplayString()}>();");
                    }
                }
            }
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
            var symbol = this.Context.Compilation.GetTypeByMetadataName(searchType);
            var types = this
                .allSymbols
                .Where(x => inherits
                    ? x.Inherits(symbol)
                    : x.Implements(symbol)
                );

            foreach (var type in types)
                action(type);
        }
    }
}
