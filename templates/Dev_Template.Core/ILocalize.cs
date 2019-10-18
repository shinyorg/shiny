using System;


namespace $ext_safeprojectname$
{
    // <Label Text="{Binding Localize[MyKey]}" />
    public interface ILocalize
    {
        string this[string key] { get; }
        string GetEnumString(Enum value);
    }
}
