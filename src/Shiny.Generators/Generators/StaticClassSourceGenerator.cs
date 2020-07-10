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
            var symbol = context.Compilation.GetTypeByMetadataName("Shiny.Generators.AutoShinyStartupAttribute");
            var attribute = context.Compilation.Assembly.FindAttributeFlattened(symbol);
            if (attribute == null || (bool)attribute.ConstructorArguments[0].Value == false)
                return;

            BuildStaticClass(context, "Shiny.Beacons.IBeaconRangingManager", "ShinyBeaconRangingManager", "Shiny.Beacons");
            BuildStaticClass(context, "Shiny.Beacons.IBeaconMonitoringManager", "ShinyBeaconMonitoringManager", "Shiny.Beacons");

            BuildStaticClass(context, "Shiny.BluetoothLE.IBleManager", "ShinyBleManager", "Shiny.BluetoothLE");
            BuildStaticClass(context, "Shiny.BluetoothLE.Hosting.IBleHostingManager", "ShinyBleHostingManager", "Shiny.BluetoothLE.Hosting");
            BuildStaticClass(context, "Shiny.Net.Http.IHttpTransferManager", "ShinyHttpTransferManager", "Shiny.Net.Http");

            // TODO: notifications
            // TODO: core - settings, job manager, connectivity, power, 
            // TODO: locations - geofencing, gps, motion activity
            // TODO: nfc
            // TODO: push
            // TODO: sensors
            BuildStaticClass(context, "Shiny.SpeechRecognition.ISpeechRecognizer", "ShinySpeechRecognizer", "Shiny.SpeechRecognition");

            // TODO: ble hosting
            // TODO: location sync
            // TODO: media manager
            // TODO: trip tracker
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
                    builder.AppendLine($"public static {ifTypeName} Current => ShinyHost.Resolve<{ifTypeName}>();");
                    builder.AppendLine();

                    foreach (var method in type.GetMethods().Where(x => !x.IsProperty()))
                    {
                        var argList = method.BuildArgString(true);
                        var argListNoNames = method.BuildArgString(false);

                        var returnType = method.ReturnType?.ToDisplayString() ?? "void";
                        builder.AppendLineInvariant($"public static {returnType} {method.Name}({argList}) => Current.{method.Name}({argListNoNames});");
                    }

                    foreach (var prop in type.GetProperties())
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
