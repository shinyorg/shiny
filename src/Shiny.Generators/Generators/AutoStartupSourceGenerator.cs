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
                    using (builder.BlockInvariant("public override void ConfigureServices(IServiceCollection services)"))
                    {
                        builder.AppendLineInvariant("this.CustomConfigureServices(services)");

                        context.RegisterIf(builder, "Shiny.BluetoothLE.IBleManager", "services.UseBleClient();");
                        context.RegisterIf(builder, "Shiny.BluetoothLE.Hosting.IBleHostingManager", "services.UseBleHosting();");

                        context.RegisterIf(builder, "Shiny.Beacons.Beacon", "services.UseBeaconRanging();");

                        // TODO: if a delegate is found
                        //context.RegisterIf(builder, "Shiny.Beacons.Beacon", "services.UseBeaconMonitoring();");

                        context.RegisterIf(builder, "Shiny.Locations.GpsModule", "services.UseMotionActivity();");
                        // TODO: geofences and gps

                        context.RegisterIf(builder, "Shiny.Net.Http", "");
                        context.RegisterIf(builder, "", "");
                        // TODO: 2 managers - check and find delegate
                        //context.RegisterIf(builder, "Shiny.Locations.Sync.ILocationSyncManager", "");

                        // TODO: optional delegate
                        //context.RegisterIf(builder, "Shiny.MediaSync.IMediaSyncManager", "services.UseMediaSync()");

                        // TODO: delegate
                        //context.RegisterIf(builder, "Shiny.TripTracker.ITripTrackerManager", "services.UseTripTracker()");

                        // TODO: auto find delegates where necessary like "geofencing" and then register the rest
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
