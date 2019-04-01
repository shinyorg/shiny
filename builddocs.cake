#tool nuget:?package=Wyam
#addin nuget:?package=Cake.Wyam

//https://wyam.io/docs/deployment/cake
//https://wyam.io/docs/usage/command-line
Task("Build")
    .Does(() =>
    {
        Wyam(new WyamSettings
        {
            NoClean = false,
            Recipe = "Docs",
            Theme = "Samson",
            UpdatePackages = true
        });
    });

Task("Preview")
    .Does(() =>
    {
        Wyam(new WyamSettings
        {
            NoClean = false,
            Recipe = "Docs",
            Theme = "Samson",
            Preview = true,
            Watch = true
        });
    });