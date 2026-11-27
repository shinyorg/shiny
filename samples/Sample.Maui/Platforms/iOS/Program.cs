using System.Diagnostics.CodeAnalysis;
using UIKit;

namespace Sample.Maui;

public class Program
{
    // SceneDelegate is only named in Info.plist, so the trimmer (PublishAot) would remove it and iOS would connect a
    // scene with no delegate - a black screen with no error
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(SceneDelegate))]
    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }
}
