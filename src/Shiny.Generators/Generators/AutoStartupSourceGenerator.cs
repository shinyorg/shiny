using System;
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
            if (!context.HasAssemblyAttribute(typeof(AutoShinyStartupAttribute).FullName))
            {
                log.Info("Assembly is not setup to auto-build the Shiny Startup");
                return;
            }
            log.Info("Generating Shiny Startup");

            // TODO: what about custom attributes?
            // TODO: search through assemblies for shiny IMPLEMENTATIONS, if found, create a startup?
            // TODO: if assembly marked with auto di, create a start class?
            // TODO: hunt for any jobs, modules, or startup tasks and register

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

                        context.RegisterIf(builder, "Shiny.Locations.GpsModule", "services.UseMotionActivity(); // TODO: UseGps<?> & UseGeofencing<>");

                        context.RegisterIf(builder, "Shiny.Net.Http", "// TODO: services.UseHttpTransfers();");

                        // TODO: 2 managers - check and find delegate
                        context.RegisterIf(builder, "Shiny.Locations.Sync.ILocationSyncManager", "// TODO: services.UseGpsSync(); or geofence");

                        // TODO: optional delegate
                        context.RegisterIf(builder, "Shiny.MediaSync.IMediaSyncManager", "//TODO: services.UseMediaSync();");

                        // TODO: delegate
                        context.RegisterIf(builder, "Shiny.TripTracker.ITripTrackerManager", "//TODO: services.UseTripTracker();");

                        // TODO: what about has assembly instead of type finding?
                        context.RegisterIf(builder, "Shiny.Sensors.ISensor", "services.UseAllSensors();");

                        // TODO: nfc
                        // TODO: notifications with or without delegate?
                    }
                },
                nameSpace, 
                "AppShinyStartup", 
                "Shiny.ShinyStartup"
            );

            context.AddCompilationUnit("AppShinyStartup", builder.ToString());
        }
    }
}
