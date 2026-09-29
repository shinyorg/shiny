using System;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Shiny.AppFunctions.Build;

/// <summary>
/// Reads the constants the Shiny.AppFunctions source generator put in the compiled assembly
/// (Shiny.AppFunctions.Generated.__ShinyAppFunctionsManifest) and writes them out as the Swift intents and the Android
/// AppFunctions schema. Source generators can only emit C#, so this is how the other languages leave the compiler.
/// Files are only rewritten when their content changes, so incremental native/asset steps stay incremental.
/// </summary>
public class ExtractAppFunctionsManifest : Task
{
    const string Namespace = "Shiny.AppFunctions.Generated";
    const string TypeName = "__ShinyAppFunctionsManifest";

    [Required] public string Assembly { get; set; } = "";
    [Required] public string OutputDirectory { get; set; } = "";

    /// <summary>False when the app declares no functions (the generator emitted nothing).</summary>
    [Output] public bool HasFunctions { get; set; }
    [Output] public string SwiftFile { get; set; } = "";
    [Output] public string AndroidFunctionsFile { get; set; } = "";
    [Output] public string AndroidFunctionsV2File { get; set; } = "";

    public override bool Execute()
    {
        Directory.CreateDirectory(this.OutputDirectory);
        this.SwiftFile = Path.Combine(this.OutputDirectory, "ShinyAppFunctions.Generated.swift");
        this.AndroidFunctionsFile = Path.Combine(this.OutputDirectory, "app_functions.xml");
        this.AndroidFunctionsV2File = Path.Combine(this.OutputDirectory, "app_functions_v2.xml");

        string? swift = null, androidV1 = null, androidV2 = null;
        try
        {
            using var stream = File.OpenRead(this.Assembly);
            using var pe = new PEReader(stream);
            var md = pe.GetMetadataReader();

            var manifest = md.TypeDefinitions
                .Select(md.GetTypeDefinition)
                .Where(t => md.GetString(t.Name) == TypeName && md.GetString(t.Namespace) == Namespace);

            foreach (var type in manifest)
            {
                var constants = type
                    .GetFields()
                    .Select(md.GetFieldDefinition)
                    .Select(f => (Field: f, Constant: f.GetDefaultValue()))
                    .Where(x => !x.Constant.IsNil)
                    .Select(x => (x.Field, Constant: md.GetConstant(x.Constant)))
                    .Where(x => x.Constant.TypeCode == ConstantTypeCode.String);

                foreach (var (field, c) in constants)
                {
                    var value = md.GetBlobReader(c.Value).ReadConstant(ConstantTypeCode.String) as string;
                    switch (md.GetString(field.Name))
                    {
                        case "Swift": swift = value; break;
                        case "AndroidFunctions": androidV1 = value; break;
                        case "AndroidFunctionsV2": androidV2 = value; break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            this.Log.LogError("Shiny.AppFunctions: could not read {0}: {1}", this.Assembly, ex.Message);
            return false;
        }

        this.HasFunctions = swift != null;
        if (!this.HasFunctions)
            this.Log.LogMessage(MessageImportance.Normal, "Shiny.AppFunctions: no app functions in {0}", Path.GetFileName(this.Assembly));

        // always write: the iOS build links the Swift runtime either way, and the assets must exist on Android
        WriteIfChanged(this.SwiftFile, swift ?? "// no app functions declared\nimport AppIntents\n");
        WriteIfChanged(this.AndroidFunctionsFile, androidV1 ?? "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<appfunctions />\n");
        WriteIfChanged(this.AndroidFunctionsV2File, androidV2 ?? "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>\n<appfunctions />\n");
        return !this.Log.HasLoggedErrors;
    }

    static void WriteIfChanged(string path, string content)
    {
        if (File.Exists(path) && File.ReadAllText(path) == content)
            return;
        File.WriteAllText(path, content, new UTF8Encoding(false));
    }
}
