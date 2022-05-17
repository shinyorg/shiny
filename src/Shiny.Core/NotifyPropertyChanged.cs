using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace Shiny;


public class NotifyPropertyChanged : INotifyPropertyChanged
{

    PropertyChangedEventHandler? handler;
    public event PropertyChangedEventHandler? PropertyChanged
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


    /// <summary>
    /// This will run as consumers hook or all unhook from the PropertyChanged event
    /// </summary>
    /// <param name="hasSubscribers"></param>
    protected virtual void OnNpcHookChanged(bool hasSubscribers)
    {
    }


    /// <summary>
    /// Returns true if anyone is hooked to PropertyChanged
    /// </summary>
    protected bool HasSubscribers => this.handler != null;


    /// <summary>
    /// Manually raise a PropertyChanged event for the caller member name or set property name
    /// </summary>
    /// <param name="propertyName"></param>
    protected virtual void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
        => this.handler?.Invoke(this, new PropertyChangedEventArgs(propertyName));


    protected virtual void RaisePropertyChanged<T>(Expression<Func<T>> expression)
    {
        var member = expression.Body as MemberExpression;
        if (member == null)
            throw new ArgumentException("Expression is not a MemberExpression");

        this.RaisePropertyChanged(member.Member.Name);
    }


    /// <summary>
    /// Sets the property if the value does not match equality
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="property"></param>
    /// <param name="value"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    protected virtual bool Set<T>(ref T property, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(property, value))
            return false;

        property = value;
        this.RaisePropertyChanged(propertyName);
        return true;
    }
}
