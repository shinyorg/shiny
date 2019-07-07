using System;
using System.ComponentModel;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shiny;

namespace Samples.ShinySetup
{
    public interface IFullService : INotifyPropertyChanged, IStartupTask
    {

    }


    public class FullService : ReactiveObject, IFullService
    {
        [Reactive] public int Count { get; set;}
        public void Start()
        {
            this.Count++;
            Console.WriteLine("Startup Count today is " + this.Count);
        }
    }
}
