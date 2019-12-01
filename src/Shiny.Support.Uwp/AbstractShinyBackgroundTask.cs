using System;
using Windows.ApplicationModel.Background;


namespace Shiny
{
    public abstract class AbstractShinyBackgroundTask : IBackgroundTask
    {
        readonly string typeName;
        readonly string methodName;


        protected AbstractShinyBackgroundTask(string typeName, string methodName)
        {
            this.typeName = typeName;
            this.methodName = methodName;
        }


        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var host = Type.GetType(this.typeName);
            var method = host.GetMethod(this.methodName);
            method.Invoke(host, new[] { taskInstance });
        }
    }
}
