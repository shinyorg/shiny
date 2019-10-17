using System;
namespace Shiny.Infrastructure
{
    public abstract class ShinyServiceModuleAttribute : ServiceModuleAttribute
    {
        protected ShinyServiceModuleAttribute(Type delegateType, bool delegateRequired)
        {
            this.DelegateType = delegateType;
            this.IsDelegateRequired = delegateRequired;
        }


        public Type DelegateType { get; }
        public bool IsDelegateRequired { get; }
    }
}
