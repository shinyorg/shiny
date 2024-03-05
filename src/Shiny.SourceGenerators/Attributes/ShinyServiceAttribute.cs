namespace Shiny.Auto;


[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public class ShinyServiceAttribute(ServiceLifetime Lifetime = ServiceLifetime.Singleton) : Attribute
{
	// INPC
	// IShinyStartupTask
}

public enum ServiceLifetime
{
	Singleton,
	Transient,
	Scoped
}