using System;
using Shiny;


namespace Samples.ShinySetup
{
    public class StartupTask1 : IStartupTask
    {
        public StartupTask1()
        {

        }

        public void Start()
        {
            Console.WriteLine("Hi From " + this.GetType().Name);
        }
    }


    public class StartupTask2 : IStartupTask
    {
        public StartupTask2()
        {

        }
        public void Start()
        {
            Console.WriteLine("Hi From " + this.GetType().Name);
        }
    }
}
