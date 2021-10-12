Title: Localization
Order: 5
---

```csharp
public class SampleStartup : FrameworkStartup
{
    protected override void Configure(ILoggingBuilder builder, IServiceCollection services)
    {
        services.UseResxLocalization(this.GetType().Assembly, "FullNamespace.ToYourResx");
    }
}
```


Enums
```csharp
For enums, use the full namespace plus the name of the value to translate

namespace MyNamespace
{
    public enum MyEnum {
        Value1,
        Value2
    }
}

Key: MyNamespace.MyEnum.Value1 - Value: One
Key: MyNamespace.MyEnum.Value2 - Value: Two

var value = ShinyHost.Resolve<ILocalize>().GetEnum<MyEnum>(MyEnum.Value);
// value is now One
```