using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Shiny.Jobs;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;


namespace Shiny.Generators.Generators
{
    public static class AutoStartupSourceGenerator
    {
        public static bool IsGenerated { get; private set; }


        public static void Execute(SourceGeneratorContext context)
        {
            //System.Diagnostics.Debugger.Launch();

            var log = context.GetLogger();
            if (!context.HasAssemblyAttribute(typeof(GenerateStartupAttribute).FullName))
            {
                log.Info("Assembly is not setup to auto-build the Shiny Startup");
                return;
            }
            log.Info("Generating Shiny Startup");

            IsGenerated = true;
            var nameSpace = context.GetProjectInstance().GetPropertyValue("RootNamespace");
            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces("Microsoft.Extensions.DependencyInjection");
            
            builder.CreateClass(
                () =>
                {
                    builder.AppendLine("protected virtual void CustomConfigureServices(IServiceCollection services) {}");
                    builder.AppendLine();

                    using (builder.BlockInvariant("public override void ConfigureServices(IServiceCollection services)"))
                    {
                        builder.AppendLineInvariant("this.CustomConfigureServices(services);");

                        RegisterIf(context, builder, "Shiny.BluetoothLE.Hosting.IBleHostingManager", "services.UseBleHosting();");
                        RegisterIf(context, builder, "Shiny.Nfc.INfcManager", "services.UseNfc();");
                        RegisterIf(context, builder, "Shiny.Sensors.IAccelerometer", "services.UseAllSensors();");
                        RegisterIf(context, builder, "Shiny.SpeechRecognition.ISpeechRecognizer", "services.UseSpeechRecognition();");
                        RegisterIf(context, builder, "Shiny.Locations.IGpsManager", "services.UseMotionActivity();");

                        RegisterAllDelegate(context, builder, "Shiny.Locations.IGpsDelegate", "services.UseGps", false);
                        RegisterAllDelegate(context, builder, "Shiny.Locations.IGeofenceDelegate", "services.UseGeofencing", true);
                        RegisterAllDelegate(context, builder, "Shiny.BluetoothLE.IBleDelegate", "services.UseBleClient", false);
                        RegisterAllDelegate(context, builder, "Shiny.Notifications.INotificationDelegate", "services.UseNotifications", false);
                        RegisterAllDelegate(context, builder, "Shiny.MediaSync.IMediaSyncDelegate", "services.UseMediaSync", false);
                        RegisterAllDelegate(context, builder, "Shiny.Net.Http.IHttpTransferDelegate", "services.UseHttpTransfers", true);
                        RegisterAllDelegate(context, builder, "Shiny.Beacons.IBeaconMonitorDelegate", "services.UseBeaconRanging", true);
                        RegisterAllDelegate(context, builder, "Shiny.Locations.Sync.IGeofenceSyncDelegate", "services.UseGeofencingSync", true);
                        RegisterAllDelegate(context, builder, "Shiny.Locations.Sync.IGpsSyncDelegate", "services.UseGpsSync", true);
                        RegisterAllDelegate(context, builder, "Shiny.TripTracker.ITripTrackerDelegate", "services.UseTripTracker", true);

                        RegisterJobs(context, builder);
                        RegisterStartupTasks(context, builder);
                        RegisterModules(context, builder);
                        RegisterInjects(context, builder);
                    }
                    
                    if (context.HasXamarinForms())
                    {
                        using (builder.BlockInvariant("public override void ConfigureApp(IServiceProvider provider)"))
                        {
                            builder.AppendFormatInvariant("Xamarin.Forms.Internals.DependencyResolver.ResolveUsing(t => provider.GetService(t));");
                            builder.AppendLine();
                        }
                    }
                },
                nameSpace, 
                "AppShinyStartup", 
                "Shiny.ShinyStartup"
            );

            context.AddCompilationUnit("AppShinyStartup", builder.ToString());
        }


        static void RegisterIf(SourceGeneratorContext context, IndentedStringBuilder builder, string typeNameExists, string registerString)
        {
            var symbol = context.Compilation.GetTypeByMetadataName(typeNameExists);
            if (symbol != null)
            {
                context.GetLogger().Info("Registering in Shiny Startup - " + registerString);
                builder.AppendLineInvariant(registerString);
            }
        }


        static void RegisterAllDelegate(SourceGeneratorContext context, IIndentedStringBuilder builder, string delegateTypeName, string registerStatement, bool oneDelegateRequiredToInstall)
        {
            var symbol = context.Compilation.GetTypeByMetadataName(delegateTypeName);
            if (symbol == null)
                return;

            var impls = context
                .GetAllImplementationsOfType(delegateTypeName)
                .WhereNotSystem()
                .ToArray();

            if (!impls.Any() && oneDelegateRequiredToInstall)
                return;

            if (oneDelegateRequiredToInstall)
                registerStatement += $"<{impls.First().ToDisplayString()}>";

            registerStatement += "();";
            builder.AppendLineInvariant(registerStatement);

            if (impls.Length > 1)
            {
                var startIndex = oneDelegateRequiredToInstall ? 1 : 0; 
                for (var i = startIndex; i < impls.Length; i++)
                {
                    var impl = impls[i];
                    builder.AppendLineInvariant($"services.AddSingleton<{delegateTypeName}, {impl.ToDisplayString()}>();");
                }
            }
        }


        static void RegisterInjects(SourceGeneratorContext context, IndentedStringBuilder builder)
        {
            var attribute = context.Compilation.GetTypeByMetadataName("Shiny.Generators.ShinyInjectAttribute");
            var injects = context.Compilation.Assembly.GetAllAttributes().Where(x => x.AttributeClass.Equals(attribute));

            foreach (var inject in injects)
            {
                var type1 = (INamedTypeSymbol)inject.ConstructorArguments[0].Value;
                var type2 = (INamedTypeSymbol)inject.ConstructorArguments[1].Value;
                builder.AppendLineInvariant($"services.AddSingleton<{type1.ToDisplayString()}, {type2.ToDisplayString()}>();");
            }
        }


        static void RegisterJobs(SourceGeneratorContext context, IndentedStringBuilder builder)
        {
            var jobTypes = context
                .GetAllImplementationsOfType<IJob>()
                .WhereNotSystem();

            foreach (var type in jobTypes)
                builder.AppendLineInvariant($"services.RegisterJob(typeof({type.ToDisplayString()}));");
        }


        static void RegisterStartupTasks(SourceGeneratorContext context, IndentedStringBuilder builder)
        {
            var types = context
                .GetAllImplementationsOfType<IShinyStartupTask>()
                .WhereNotSystem()
                .Where(x => x.AllInterfaces.Length == 1);

            foreach (var task in types)
                builder.AppendLineInvariant($"services.AddSingleton<Shiny.IShinyStartupTask, {task.ToDisplayString()}>();");
        }


        static void RegisterModules(SourceGeneratorContext context, IndentedStringBuilder builder)
        {
            var types = context
                .GetAllImplementationsOfType<IShinyModule>()
                .WhereNotSystem();

            foreach (var type in types)
                builder.AppendLineInvariant($"services.RegisterModule<{type.ToDisplayString()}>();");
        }
    }
}
