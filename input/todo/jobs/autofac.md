## Dependency Injection - Autofac

Install [![NuGet](https://img.shields.io/nuget/v/Plugin.Jobs.Autofac.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.Jobs.Autofac/)

Make sure to call CrossJobs.Init(..); on the respective platform even though you are doing DI.  There are some internals that still need initialization!

### Registering the JobManager on your container (IJobManager)
```csharp
var builder = new ContainerBuilder();

// This registers IJobManager on your container as well as setting it on CrossJobs.Current
builder.RegisterJobManager();
```

### Registering Known Jobs On Your Container 
```csharp
// This is a nice way to not only register your job (with custom dependencies on the container), but also set it up with the job framework
// NOTE: no need to set the JobInfo.Type or worry about scheduling a job that already exists, we just update the details
// NOTE: if you cancel jobs in the job manager, you must register them using the standard IJobManager.Schedule even though you used the ContainerBuilder.Schedule previously

var builder = new ContainerBuilder();
builder.Schedule(new JobInfo 
{
    Name = "MyJob"
    // any additional criteria
})

```

### Registering Static Job for multi-use

```csharp
var builder = new ContainerBuilder();

// Dynamically setting up jobs with parameters, no problem - just use
builder.RegisterJob<MyJob>();

// then do the following later when you are registering your job or using it with multiple parameter sets
container.Resolve<IJobManager>().Schedule(new JobInfo 
{
    Name = "MyJob",
    Type = typeof(MyJob)
});
```