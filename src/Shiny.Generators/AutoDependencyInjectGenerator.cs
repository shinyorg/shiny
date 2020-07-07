using System;
using System.Linq;

using Uno.RoslynHelpers;
using Uno.SourceGeneration;


namespace Shiny.Generators
{
    public class AutoDependencyInjectGenerator : SourceGenerator
    {
        public override void Execute(SourceGeneratorContext context)
        {
            // TODO: find the autogen assembly attribute?
            // TODO: what about custom attributes?
            // TODO: search through assemblies for shiny IMPLEMENTATIONS, if found, create a startup?
            // TODO: if assembly marked with auto di, create a start class?
            // TODO: hunt for any jobs, modules, or startup tasks and register

            var builder = new IndentedStringBuilder();
            builder.AppendLineInvariant("using System;");
            builder.AppendLineInvariant("using Shiny;");
            builder.AppendLineInvariant("Microsoft.Extensions.DependencyInjection");
            
            var nameSpace = context
                .GetProjectInstance()
                .Properties
                .FirstOrDefault(x => x.Name.Equals(
                    "rootnamespace", 
                    StringComparison.InvariantCultureIgnoreCase
                ));

            using (builder.BlockInvariant($"namespace {nameSpace}"))
            {
                builder.Append("protected virtual void CustomConfigureServices(IServiceCollection services) {}");

                using (builder.BlockInvariant("public partial class AppShinyStartup : Shiny.ShinyStartup"))
                {
                    using (builder.BlockInvariant("public override void ConfigureServices(IServiceCollection services)"))
                    {
                        builder.AppendLineInvariant("this.CustomConfigureServices(services)");

                        context.RegisterIf(builder, "Shiny.BluetoothLE.IBleManager", "services.UseBleClient();");
                        context.RegisterIf(builder, "Shiny.BluetoothLE.Hosting.IBleHostingManager", "services.UseBleClient();");

                        context.RegisterIf(builder, "Shiny.Beacons.Beacon", "services.UseBeaconRanging();");
                        
                        // TODO: if a delegate is found
                        //context.RegisterIf(builder, "Shiny.Beacons.Beacon", "services.UseBeaconMonitoring();");

                        context.RegisterIf(builder, "", "");
                        context.RegisterIf(builder, "", "");
                        context.RegisterIf(builder, "", "");
                        // TODO: 2 managers - check and find delegate
                        //context.RegisterIf(builder, "Shiny.Locations.Sync.ILocationSyncManager", "");

                        // TODO: optional delegate
                        //context.RegisterIf(builder, "Shiny.MediaSync.IMediaSyncManager", "services.UseMediaSync()");

                        // TODO: delegate
                        //context.RegisterIf(builder, "Shiny.TripTracker.ITripTrackerManager", "services.UseTripTracker()");

                        // TODO: auto find delegates where necessary like "geofencing" and then register the rest
                    }
                }
            }
            context.AddCompilationUnit("AppShinyStartup", builder.ToString());
        }
    }
}
