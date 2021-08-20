using Nuke.Common;
using Nuke.Common.ProjectModel;
using static Nuke.Common.ValueInjection.ValueInjectionUtility;


interface ISolutionAccess : INukeBuild
{
    [Solution] [Required] Solution Solution => TryGetValue(() => Solution);
}