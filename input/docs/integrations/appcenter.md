<!--
This file was generate by MarkdownSnippets.
Source File: /input/docs/integrations/appcenter.source.md
To change this file edit the source file and then re-run the generation using either the dotnet global tool (https://github.com/SimonCropp/MarkdownSnippets#markdownsnippetstool) or using the api (https://github.com/SimonCropp/MarkdownSnippets#running-as-a-unit-test).
-->
Title: App Center Logging
---

# SETUP

Install from NuGet: [![NuGet](https://img.shields.io/nuget/v/Shiny.Logging.AppCenter.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Logging.AppCenter/)


## Setup

<!-- snippet: AppCenterStartup.cs -->
```cs
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class AppCenterStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseAppCenterLogging("android=YourAppSecret;ios=YourAppSecret");
    }
}
```
<sup>[snippet source](/src/Snippets/AppCenterStartup.cs#L1-L10)</sup>
<!-- endsnippet -->
