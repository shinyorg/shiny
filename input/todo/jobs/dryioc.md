## DryIoC

Install [![NuGet](https://img.shields.io/nuget/v/Plugin.Jobs.DryIoc.svg?maxAge=2592000)](https://www.nuget.org/packages/Plugin.Jobs.DryIoc/)


### Registering the JobManager on your container (IJobManager)
```csharp
var container = new Container();

// This registers IJobManager on your container as well as setting it on CrossJobs.Current
container.RegisterJobManager();
```

### Registering Known Jobs On Your Container 
```csharp
// This is a nice way to not only register your job (with custom dependencies on the container), but also set it up with the job framework
// NOTE: no need to set the JobInfo.Type or worry about scheduling a job that already exists, we just update the details
// NOTE: if you cancel jobs in the job manager, you must register them using the standard IJobManager.Schedule even though you used the ContainerBuilder.Schedule previously

var container = new Container();
builder.Schedule(new JobInfo 
{
    Name = "MyJob"
    // any additional criteria
})

```
