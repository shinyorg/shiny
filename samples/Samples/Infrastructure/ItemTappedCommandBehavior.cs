using System;
using System.Windows.Input;
using Xamarin.Forms;


namespace Samples.Infrastructure
{
    public class ItemTappedCommandBehavior : AbstractBindableBehavior<ListView>
    {
        public static readonly BindableProperty CommandProperty = BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(ItemTappedCommandBehavior));
        public ICommand Command
        {
            get => (ICommand)this.GetValue(CommandProperty);
            set => this.SetValue(CommandProperty, value);
        }


        protected override void OnAttachedTo(ListView bindable)
        {
            base.OnAttachedTo(bindable);
            this.AssociatedObject.ItemTapped += this.OnItemTapped;
        }


        protected override void OnDetachingFrom(ListView bindable)
        {
            base.OnDetachingFrom(bindable);
            this.AssociatedObject.ItemTapped -= this.OnItemTapped;
        }


        void OnItemTapped(object sender, ItemTappedEventArgs e)
        {
            if (this.Command == null || e.Item == null)
                return;

            if (this.Command.CanExecute(e.Item))
                this.Command.Execute(e.Item);
        }
    }
}