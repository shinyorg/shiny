using System;
using System.Linq;
using Shiny.Jobs;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;


namespace Shiny.Generators.Generators
{
    public static class AutoStartupSourceGenerator
    {
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

                        context.RegisterIf(builder, "Shiny.BluetoothLE.IBleManager", "services.UseBleClient();");
                        context.RegisterIf(builder, "Shiny.BluetoothLE.Hosting.IBleHostingManager", "services.UseBleHosting();");
                        context.RegisterIf(builder, "Shiny.Beacons.Beacon", "services.UseBeaconRanging(); // TODO: UseBeaconMonitoring<?>");
                        context.RegisterIf(builder, "Shiny.Locations.IGpsManager", "services.UseMotionActivity(); // TODO: UseGps<?> & UseGeofencing<>");
                        context.RegisterIf(builder, "Shiny.Net.Http", "// TODO: services.UseHttpTransfers();");
                        context.RegisterIf(builder, "Shiny.Nfc.INfcManager", "services.UseNfc();");
                        context.RegisterIf(builder, "Shiny.Sensors.IAccelerometer", "services.UseAllSensors();");
                        context.RegisterIf(builder, "Shiny.SpeechRecognition.ISpeechRecognizer", "services.UseSpeechRecognition();");

                        // TODO: notifications with or without delegate?
                        context.RegisterIf(builder, "Shiny.Notifications.INotificationManager", "//TODO: services.UseNotifications();");

                        // TODO: 2 managers - check and find delegate
                        context.RegisterIf(builder, "Shiny.Locations.Sync.ILocationSyncManager", "// TODO: services.UseGpsSync(); or geofence");

                        // TODO: optional delegate
                        context.RegisterIf(builder, "Shiny.MediaSync.IMediaSyncManager", "//TODO: services.UseMediaSync();");

                        // TODO: delegate
                        context.RegisterIf(builder, "Shiny.TripTracker.ITripTrackerManager", "//TODO: services.UseTripTracker();");

                        RegisterJobs(context, builder);
                        RegisterStartupTasks(context, builder);
                        RegisterModules(context, builder);
                    }
                },
                nameSpace, 
                "AppShinyStartup", 
                "Shiny.ShinyStartup"
            );

            context.AddCompilationUnit("AppShinyStartup", builder.ToString());
        }


        static void RegisterJobs(SourceGeneratorContext context, IndentedStringBuilder builder)
        {
            var jobTypes = context.GetAllImplementationsOfType<IJob>();

            foreach (var type in jobTypes)
                builder.AppendLine($"services.RegisterJob(typeof({type.ToDisplayString()}));");
        }


        static void RegisterStartupTasks(SourceGeneratorContext context, IndentedStringBuilder builder)
        {
            var types = context
                .GetAllImplementationsOfType<IShinyStartupTask>()
                .Where(x => x.AllInterfaces.Length == 1);

            foreach (var task in types)
                builder.AppendLine($"services.AddSingleton<Shiny.IShinyStartupTask, {task.ToDisplayString()}>();");
        }


        static void RegisterModules(SourceGeneratorContext context, IndentedStringBuilder builder)
        {
            var types = context.GetAllImplementationsOfType<IShinyModule>();

            foreach (var type in types)
                builder.AppendLine($"services.RegisterModule<{type.ToDisplayString()}>();");
        }


        //static void RegisterIfAndWithDelegates(SourceGeneratorContext context, IndentedStringBuilder builder, string initialRegisterString, string delegateTypeName)
        //{
        //    var delegateType = context.Compilation.GetTypeByMetadataName(delegateTypeName);
        //    if (delegateType == null)
        //        return;
        //}
    }
}
