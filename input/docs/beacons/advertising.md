<!--
This file was generate by MarkdownSnippets.
Source File: /input/docs/beacons/advertising.source.md
To change this file edit the source file and then re-run the generation using either the dotnet global tool (https://github.com/SimonCropp/MarkdownSnippets#markdownsnippetstool) or using the api (https://github.com/SimonCropp/MarkdownSnippets#running-as-a-unit-test).
-->
Title: Advertising
---

|Platform|Version|
|--------|-------|
|iOS|9|
|Android|5|
|UWP|16299|

[![NuGet](https://img.shields.io/nuget/v/Shiny.Beacons.Advertising.svg?maxAge=2592000)](https://www.nuget.org/packages/Shiny.Beacons.Advertising/)


# TODO

<!-- snippet: BeaconAdsStartup.cs -->
```cs
using Microsoft.Extensions.DependencyInjection;
using Shiny;

public class BeaconAdsStartup : ShinyStartup
{
    public override void ConfigureServices(IServiceCollection services)
    {
        services.UseBeaconAdvertising();
    }
}
```
<sup>[snippet source](/src/Snippets/BeaconAdsStartup.cs#L1-L10)</sup>
<!-- endsnippet -->


