Title: Using Shiny without Dependency Injection
---

Can it be done?  The answer is absolutely.  As with almost all DI based frameworks, there is a static variable with the container object somewhere.  For Shiny, it is as simple as the following:

'''csharp
Shiny.ShinyHost.Resolve
'''

There are also shims setup for the more classic way of using plugins