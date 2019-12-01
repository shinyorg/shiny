using System;
using Windows.ApplicationModel.Background;


namespace Shiny
{
    static class Router
    {
        public static void Run(IBackgroundTaskInstance taskInstance, string classType, string methodName)
        {
            var host = Type.GetType(classType);
            var method = host.GetMethod(methodName);
            method.Invoke(host, new[] { taskInstance });
        }
    }
}
