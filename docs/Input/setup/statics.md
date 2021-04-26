Title: Static Class Generation
---
# Statics

Using Shiny without dependency injection is not only possible, it is quite easy to do.  Do also you like the old plugin style of CrossPlugin.Current?  Then you've come to the right document.  You may be thinking, why not just include these as part of the package?  Several reasons actually, first being - statics don't make as easily testable and the second, statics don't adhere to interfaces and Shiny is always evolving.  I got tired of trying to keep things in sync.  By adding a generation option, it makes things good for all of us.  

Under the hood, you are still using dependency injection and you still need a startup file, BUT, you can have all of the static versions of each service generated in a project of your choice by simply adding the following to your assembly attributes:


```csharp
[assembly: Shiny.GenerateStaticClasses]
```

After adding this attribute, perform a build.  The Shiny source generator will now scan for all Shiny nuget packages that are referenced in this project and generate corresponding static classes for the main services.  

```csharp
// BEFORE
Shiny.ShinyHost.Resolve<IJobManager>().Run(...);

// AFTER
ShinyJobs.Run(...);
```


By adding this attribute, it will create static access classes for all of the Shiny services in the main namespace of the assembly you use the assembly attribute in.

<?! StaticClasses /?>