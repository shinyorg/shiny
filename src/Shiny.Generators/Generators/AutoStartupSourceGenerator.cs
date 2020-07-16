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
                        // TODO: optional delegate
                        context.RegisterIf(builder, "Shiny.MediaSync.IMediaSyncManager", "//TODO: services.UseMediaSync();");

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
                        }
                    }
                },
                nameSpace, 
                "AppShinyStartup", 
                "Shiny.ShinyStartup"
            );

            context.AddCompilationUnit("AppShinyStartup", builder.ToString());
            // TODO: inject into XF  DependencyResolver.ResolveUsing(t => ShinyHost.Container.GetService(t));
        }


        static void RegisterAllDelegate(SourceGeneratorContext context, IIndentedStringBuilder builder, string delegateTypeName, string registerStatement, bool oneDelegateRequiredToInstall)
        {
            var impls = context.GetAllImplementationsOfType(delegateTypeName).WhereNotSystem();
            if (!impls.Any() && oneDelegateRequiredToInstall)
                return;

            if (oneDelegateRequiredToInstall)
            {
                registerStatement += $"<{impls.First().ToDisplayString()}>";
            }
            registerStatement += "();";
            builder.AppendLineInvariant(registerStatement);

            // TODO: for all other impls, register
        }


        static void RegisterInjects(SourceGeneratorContext context, IndentedStringBuilder builder)
        {

        }


        static void RegisterJobs(SourceGeneratorContext context, IndentedStringBuilder builder)
        {
            var jobTypes = context.GetAllImplementationsOfType<IJob>().WhereNotSystem();

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
            var types = context.GetAllImplementationsOfType<IShinyModule>().WhereNotSystem();

            foreach (var type in types)
                builder.AppendLineInvariant($"services.RegisterModule<{type.ToDisplayString()}>();");
        }


        //static void RegisterIfAndWithDelegates(SourceGeneratorContext context, IndentedStringBuilder builder, string initialRegisterString, string delegateTypeName)
        //{
        //    var delegateType = context.Compilation.GetTypeByMetadataName(delegateTypeName);
        //    if (delegateType == null)
        //        return;
        //}
    }
}
