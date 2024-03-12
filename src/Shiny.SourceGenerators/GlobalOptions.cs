using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Shiny.SourceGenerators;


public record GlobalOptions
{
    public string? RootNamespace { get; }
    public string? ProjectFullPath { get; }
    public string? ProjectName { get; }

    public GlobalOptions(
        string? rootNamespace,
        string? projectName,
        string? projectFullPath
    )
    {
        this.RootNamespace = rootNamespace;
        this.ProjectName = projectName;
        this.ProjectFullPath = projectFullPath;
    }

    public GlobalOptions(AnalyzerConfigOptions options)
    {
        if (options.TryGetValue("build_property.MSBuildProjectFullPath", out var projectFullPath))
            this.ProjectFullPath = projectFullPath;

        if (options.TryGetValue("build_property.MSBuildProjectName", out var projectName))
            this.ProjectName = projectName;

        if (options.TryGetValue("build_property.RootNamespace", out var rootNamespace))
            this.RootNamespace = rootNamespace;
    }


    public static GlobalOptions Select(AnalyzerConfigOptionsProvider provider, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return new GlobalOptions(provider.GlobalOptions);
    }
}