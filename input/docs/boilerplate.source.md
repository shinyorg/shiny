Title: Using Shiny without the boilerplate code
---

Using Shiny without dependency injection is not only possible, it is quite easy to do.  Do also you like the old plugin style of CrossPlugin.Current?  Then you've come to the right document

Under the hood, you are still using dependency injection and you still need a startup file, BUT, you can have all of the static versions of each service generated in a project of your choice by simply adding the following to your assembly attributes:

```csharp
[assembly: Shiny.GenerateStaticClasses]
```

After adding this attribute, perform a build.  The Shiny source generator will now scan for all Shiny nuget packages that are referenced in this project and generate corresponding static classes for the main services.  

```csharp
// BEFORE
Shiny.ShinyHost.Resolve<IJobManager>().Run(...);

// AFTER
JobManager.Run(...);
```


## Startup Genration
Also, if you aren't a fan of the boilerplate startup files, Shiny can also generate them at build time for you. 

**CAUTION: this comes at a cost of customization to your startup**



```csharp
[assembly: Shiny.GenerateStartupClass]
```
