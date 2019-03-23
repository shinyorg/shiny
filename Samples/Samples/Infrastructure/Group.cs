using System;
using Shiny;


namespace Samples
{
    public class Group<T> : ObservableList<T>
    {
        public Group(string name, string shortName)
        {
            this.Name = name;
            this.ShortName = shortName;
        }


        public string Name { get; }
        public string ShortName { get; }
    }
}
