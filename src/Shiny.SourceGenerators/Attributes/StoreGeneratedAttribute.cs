namespace Shiny.Auto;

[AttributeUsage(AttributeTargets.Interface)]
public class StoreGenerateAttribute(string StoreName) : Attribute
{
}