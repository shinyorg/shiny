using System;
using Xamarin.Forms;


namespace Samples.Infrastructure
{
    public abstract class AbstractBindableBehavior<T> : Behavior<T> where T : BindableObject
    {
        public T AssociatedObject { get; private set; }

        protected override void OnAttachedTo(T bindable)
        {
            base.OnAttachedTo(bindable);
            this.AssociatedObject = bindable;

            if (bindable.BindingContext != null)
                this.BindingContext = bindable.BindingContext;

            bindable.BindingContextChanged += this.OnBindingContextChanged;
        }


        protected override void OnDetachingFrom(T bindable)
        {
            base.OnDetachingFrom(bindable);
            bindable.BindingContextChanged -= this.OnBindingContextChanged;
            this.AssociatedObject = null;
        }


        void OnBindingContextChanged(object sender, EventArgs e)
        {
            this.OnBindingContextChanged();
        }


        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();
            this.BindingContext = this.AssociatedObject.BindingContext;
        }
    }
}