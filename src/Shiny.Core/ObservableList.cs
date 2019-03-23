using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;


namespace Shiny
{
    public class ObservableList<T> : ObservableCollection<T>
    {
        public ObservableList() { }
        public ObservableList(IEnumerable<T> items) : base(items) { }


        public virtual void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
                this.Items.Add(item);

            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items));
        }


        public virtual void Repopulate(IEnumerable<T> items)
        {
            this.Clear();
            foreach (var item in items)
                this.Items.Add(item);

            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
