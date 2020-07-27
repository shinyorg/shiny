using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Uno.RoslynHelpers;
using Uno.SourceGeneration;


namespace Shiny.Generators.Generators
{
    public static class StaticClassSourceGenerator
    {
        public static void Execute(SourceGeneratorContext context)
        {
            var symbol = context.Compilation.GetTypeByMetadataName(typeof(GenerateStaticClassesAttribute).FullName);
            var attribute = context.Compilation.Assembly.FindAttributeFlattened(symbol);
            if (attribute == null)
                return;

            BuildStaticClass(context, "Shiny.Jobs.IJobManager", "ShinyJobs", "Shiny.Jobs");
            BuildStaticClass(context, "Shiny.Net.IConnectivity", "ShinyConnectivity", "Shiny.Net");
            //BuildStaticClass(context, "Shiny.Settings.ISettings", "ShinySettings", "Shiny.Settings"); // don't know how to gen generic constraints yet
            BuildStaticClass(context, "Shiny.Power.IPowerManager", "ShinyPower", "Shiny.Power");
            BuildStaticClass(context, "Shiny.IO.IFileSystem", "ShinyFileSystem", "Shiny.IO");

            BuildStaticClass(context, "Shiny.Beacons.IBeaconRangingManager", "ShinyBeaconRanging", "Shiny.Beacons");
            BuildStaticClass(context, "Shiny.Beacons.IBeaconMonitoringManager", "ShinyBeaconMonitoring", "Shiny.Beacons");
            BuildStaticClass(context, "Shiny.BluetoothLE.IBleManager", "ShinyBle", "Shiny.BluetoothLE");
            BuildStaticClass(context, "Shiny.BluetoothLE.Hosting.IBleHostingManager", "ShinyBleHosting", "Shiny.BluetoothLE.Hosting");
            BuildStaticClass(context, "Shiny.Net.Http.IHttpTransferManager", "ShinyHttpTransfers", "Shiny.Net.Http");
            BuildStaticClass(context, "Shiny.Notifications.INotificationManager", "ShinyNotifications", "Shiny.Notifications");
            BuildStaticClass(context, "Shiny.Nfc.INfcManager", "ShinyNfc", "Shiny.Nfc");
            BuildStaticClass(context, "Shiny.Push.IPushManager", "ShinyPush", "Shiny.Push");
            BuildStaticClass(context, "Shiny.Locations.IGeofenceManager", "ShinyGeofences", "Shiny.Locations");
            BuildStaticClass(context, "Shiny.Locations.IGpsManager", "ShinyGps", "Shiny.Locations");
            BuildStaticClass(context, "Shiny.Locations.IMotionActivityManager", "ShinyMotionActivity", "Shiny.Locations");
            BuildStaticClass(context, "Shiny.SpeechRecognition.ISpeechRecognizer", "ShinySpeechRecognizer", "Shiny.SpeechRecognition");

            // app services
            BuildStaticClass(context, "Shiny.Locations.Sync.ILocationSyncManager", "ShinyLocationSync", "Shiny.Locations.Sync");
            BuildStaticClass(context, "Shiny.MediaSync.IMediaSyncManager", "ShinyMediaSync", "Shiny.MediaSync");
            BuildStaticClass(context, "Shiny.TripTracker.ITripTrackerManager", "ShinyTripTracker", "Shiny.TripTracker");

            // sensors
            BuildStaticClass(context, "Shiny.Sensors.IAccelerometer", "ShinyAccelerometer", "Shiny.Sensors");
            BuildStaticClass(context, "Shiny.Sensors.IAmbientLight", "ShinyAmbientLight", "Shiny.Sensors");
            BuildStaticClass(context, "Shiny.Sensors.IBarometer", "ShinyBarometer", "Shiny.Sensors");
            BuildStaticClass(context, "Shiny.Sensors.ICompass", "ShinyCompass", "Shiny.Sensors");
            BuildStaticClass(context, "Shiny.Sensors.IGyroscope", "ShinyGyroscope", "Shiny.Sensors");
            BuildStaticClass(context, "Shiny.Sensors.IHeartRateMonitor", "ShinyHeartRate", "Shiny.Sensors");
            BuildStaticClass(context, "Shiny.Sensors.IHumidity", "ShinyHumidity", "Shiny.Sensors");
            BuildStaticClass(context, "Shiny.Sensors.IMagnetometer", "ShinyMagnetometer", "Shiny.Sensors");
            BuildStaticClass(context, "Shiny.Sensors.IPedometer", "ShinyPedometer", "Shiny.Sensors");
            BuildStaticClass(context, "Shiny.Sensors.IProximity", "ShinyProximity", "Shiny.Sensors");
            BuildStaticClass(context, "Shiny.Sensors.ITemperature", "ShinyTemperature", "Shiny.Sensors");
        }


        static void BuildStaticClass(SourceGeneratorContext context, string ifTypeName, string genFileName, string namespaces)
        {
            var type = context.Compilation.GetTypeByMetadataName(ifTypeName);
            if (type == null)
                return;

            var builder = new IndentedStringBuilder();
            builder.AppendNamespaces(namespaces);

            using (builder.BlockInvariant("namespace Shiny"))
            {
                using (builder.BlockInvariant("public static partial class " + genFileName))
                {
                    builder.AppendLine($"public static bool IsAvailable => ShinyHost.Container.IsRegistered<{ifTypeName}>();");
                    builder.AppendLine();
                    builder.AppendLine($"public static {ifTypeName} Current => ShinyHost.Resolve<{ifTypeName}>();");
                    builder.AppendLine();

                    var methods = type.GetAllPublicMethods();

                    foreach (var method in methods)
                    {
                        var argList = method.BuildArgString(true);
                        var argListNoNames = method.BuildArgString(false);

                        var returnType = method.ReturnType.IsVoid() ? "void" : method.ReturnType.ToDisplayString();
                        var signature = $"public static {returnType} {method.Name}";
                        var args = $"Current.{method.Name}";

                        if (method.IsGenericMethod)
                        {
                            signature += "<T>";
                            args += "<T>";
                        }

                        builder.AppendLineInvariant($"{signature}({argList}) => {args}({argListNoNames});");
                    }

                    foreach (var prop in type.GetAllProperties())
                    {
                        var propertyName = prop.GetName();
                        var hasGet = prop.GetMethod?.IsPublic() ?? false;
                        var hasSet = prop.SetMethod?.IsPublic() ?? false;

                        if (hasGet && !hasSet)
                        {
                            builder.AppendLineInvariant($"public static {prop.Type.ToDisplayString()} {propertyName} => Current.{propertyName};");
                        }
                        else
                        {
                            using (builder.BlockInvariant($"public static {prop.Type.ToDisplayString()} {propertyName}"))
                            {
                                if (hasGet)
                                    builder.AppendLineInvariant($"get => Current.{propertyName};");

                                if (hasSet)
                                    builder.AppendLineInvariant($"set => Current.{propertyName} = value;");
                            }
                        }
                    }
                }
            }
            context.AddCompilationUnit(genFileName, builder.ToString());
        }
    }
}
