using System;


namespace Shiny.Infrastructure
{
    public class AutoRegisterAttribute
    {
        public AutoRegisterAttribute()
        {
        }


        public Type DelegateType { get; }
        public bool DelegateRequired { get; }
    }
}
