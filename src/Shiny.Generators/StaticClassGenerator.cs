//using System;
//using Microsoft.CodeAnalysis;


//namespace Shiny.Generators.Tasks
//{
//    [Generator]
//    public class StaticClassGenerator : ISourceGenerator
//    {
//        public void Initialize(GeneratorInitializationContext context) {}


//        GeneratorExecutionContext context;
//        public void Execute(GeneratorExecutionContext context)
//        {
//            this.context = context;
//            var symbol = context.Compilation.GetTypeByMetadataName("Shiny.GenerateStaticClassesAttribute");
//            var attribute = context.Compilation.Assembly.FindAttributeFlattened(symbol);
//            if (attribute == null)
//                return;

//            this.BuildStaticClass("Shiny.Jobs.IJobManager", "ShinyJobs", "Shiny.Jobs");
//            this.BuildStaticClass("Shiny.Net.IConnectivity", "ShinyConnectivity", "Shiny.Net");
//            //this.BuildStaticClass("Shiny.Settings.ISettings", "ShinySettings", "Shiny.Settings"); // don't know how to gen generic constraints yet
//            this.BuildStaticClass("Shiny.Power.IPowerManager", "ShinyPower", "Shiny.Power");
//            this.BuildStaticClass("Shiny.IO.IFileSystem", "ShinyFileSystem", "Shiny.IO");

//            this.BuildStaticClass("Shiny.Beacons.IBeaconRangingManager", "ShinyBeaconRanging", "Shiny.Beacons");
//            this.BuildStaticClass("Shiny.Beacons.IBeaconMonitoringManager", "ShinyBeaconMonitoring", "Shiny.Beacons");
//            this.BuildStaticClass("Shiny.BluetoothLE.IBleManager", "ShinyBle", "Shiny.BluetoothLE");
//            this.BuildStaticClass("Shiny.BluetoothLE.Hosting.IBleHostingManager", "ShinyBleHosting", "Shiny.BluetoothLE.Hosting");
//            this.BuildStaticClass("Shiny.Net.Http.IHttpTransferManager", "ShinyHttpTransfers", "Shiny.Net.Http");
//            this.BuildStaticClass("Shiny.Notifications.INotificationManager", "ShinyNotifications", "Shiny.Notifications");
//            this.BuildStaticClass("Shiny.Nfc.INfcManager", "ShinyNfc", "Shiny.Nfc");
//            this.BuildStaticClass("Shiny.Push.IPushManager", "ShinyPush", "Shiny.Push");
//            this.BuildStaticClass("Shiny.Locations.IGeofenceManager", "ShinyGeofences", "Shiny.Locations");
//            this.BuildStaticClass("Shiny.Locations.IGpsManager", "ShinyGps", "Shiny.Locations");
//            this.BuildStaticClass("Shiny.Locations.IMotionActivityManager", "ShinyMotionActivity", "Shiny.Locations");
//            this.BuildStaticClass("Shiny.SpeechRecognition.ISpeechRecognizer", "ShinySpeechRecognizer", "Shiny.SpeechRecognition");

//            // app services
//            this.BuildStaticClass("Shiny.Locations.Sync.ILocationSyncManager", "ShinyLocationSync", "Shiny.Locations.Sync");
//            this.BuildStaticClass("Shiny.MediaSync.IMediaSyncManager", "ShinyMediaSync", "Shiny.MediaSync");
//            this.BuildStaticClass("Shiny.TripTracker.ITripTrackerManager", "ShinyTripTracker", "Shiny.TripTracker");
//            this.BuildStaticClass("Shiny.DataSync.IDataSyncManager", "ShinyDataSync", "Shiny.DataSync");

//            // sensors
//            this.BuildStaticClass("Shiny.Sensors.IAccelerometer", "ShinyAccelerometer", "Shiny.Sensors");
//            this.BuildStaticClass("Shiny.Sensors.IAmbientLight", "ShinyAmbientLight", "Shiny.Sensors");
//            this.BuildStaticClass("Shiny.Sensors.IBarometer", "ShinyBarometer", "Shiny.Sensors");
//            this.BuildStaticClass("Shiny.Sensors.ICompass", "ShinyCompass", "Shiny.Sensors");
//            this.BuildStaticClass("Shiny.Sensors.IGyroscope", "ShinyGyroscope", "Shiny.Sensors");
//            this.BuildStaticClass("Shiny.Sensors.IHeartRateMonitor", "ShinyHeartRate", "Shiny.Sensors");
//            this.BuildStaticClass("Shiny.Sensors.IHumidity", "ShinyHumidity", "Shiny.Sensors");
//            this.BuildStaticClass("Shiny.Sensors.IMagnetometer", "ShinyMagnetometer", "Shiny.Sensors");
//            this.BuildStaticClass("Shiny.Sensors.IPedometer", "ShinyPedometer", "Shiny.Sensors");
//            this.BuildStaticClass("Shiny.Sensors.IProximity", "ShinyProximity", "Shiny.Sensors");
//            this.BuildStaticClass("Shiny.Sensors.ITemperature", "ShinyTemperature", "Shiny.Sensors");
//        }


//        void BuildStaticClass(string ifTypeName, string genFileName, string namespaces)
//        {
//            var type = context.Compilation.GetTypeByMetadataName(ifTypeName);
//            if (type == null)
//                return;

//            var builder = new IndentedStringBuilder();
//            builder.AppendNamespaces(namespaces);

//            using (builder.BlockInvariant("namespace Shiny"))
//            {
//                using (builder.BlockInvariant("public static partial class " + genFileName))
//                {
//                    builder.AppendLine($"public static bool IsAvailable => Current != null;");
//                    builder.AppendLine();
//                    builder.AppendLine($"public static {ifTypeName} Current => ShinyHost.Resolve<{ifTypeName}>();");
//                    builder.AppendLine();

//                    var methods = type.GetAllPublicMethods();

//                    //foreach (var method in methods)
//                    //{
//                    //    var argList = method.BuildArgString(true);
//                    //    var argListNoNames = method.BuildArgString(false);

//                    //    var returnType = method.ReturnType.IsVoid() ? "void" : method.ReturnType.ToDisplayString();
//                    //    var signature = $"public static {returnType} {method.Name}";
//                    //    var args = $"Current.{method.Name}";

//                    //    if (method.IsGenericMethod)
//                    //    {
//                    //        signature += "<T>";
//                    //        args += "<T>";
//                    //    }

//                    //    builder.AppendLineInvariant($"{signature}({argList}) => {args}({argListNoNames});");
//                    //}

//                    //foreach (var prop in type.GetAllProperties())
//                    //{
//                    //    var propertyName = prop.GetName();
//                    //    var hasGet = prop.GetMethod?.IsPublic() ?? false;
//                    //    var hasSet = prop.SetMethod?.IsPublic() ?? false;

//                    //    if (hasGet && !hasSet)
//                    //    {
//                    //        builder.AppendLineInvariant($"public static {prop.Type.ToDisplayString()} {propertyName} => Current.{propertyName};");
//                    //    }
//                    //    else
//                    //    {
//                    //        using (builder.BlockInvariant($"public static {prop.Type.ToDisplayString()} {propertyName}"))
//                    //        {
//                    //            if (hasGet)
//                    //                builder.AppendLineInvariant($"get => Current.{propertyName};");

//                    //            if (hasSet)
//                    //                builder.AppendLineInvariant($"set => Current.{propertyName} = value;");
//                    //        }
//                    //    }
//                    //}
//                }
//            }
//            context.AddSource(genFileName, builder.ToString());
//        }
//    }
//}
