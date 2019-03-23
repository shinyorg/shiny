using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;


namespace Shiny
{
    public class NotifyPropertyChanged : INotifyPropertyChanged
    {

        PropertyChangedEventHandler handler;
        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                if (this.handler == null)
                    this.OnNpcHookChanged(true);

                this.handler += value;
            }
            remove
            {
                if (this.handler != null)
                    this.handler -= value;

                if (this.handler == null)
                    this.OnNpcHookChanged(false);
            }
        }


        protected virtual void OnNpcHookChanged(bool hasSubscribers)
        {
        }


        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => this.handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));


        protected virtual void OnPropertyChanged<T>(Expression<Func<T>> expression)
        {
            var member = expression.Body as MemberExpression;
            if (member == null)
                throw new ArgumentException("Expression is not a MemberExpression");

            this.OnPropertyChanged(member.Member.Name);
        }


        protected virtual bool Set<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(property, value))
                return false;

            property = value;
            this.OnPropertyChanged(propertyName);
            return true;
        }
    }
}
