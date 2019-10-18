using System;
using System.Resources;

namespace $ext_projectname$.Localization.Resx
{
    public class ResxLocalize : ILocalize
    {
        readonly ResourceManager resourceManager;


        public ResxLocalize()
        {
            this.resourceManager = new ResourceManager("$ext_projectname$.Localization.Resx.Strings", this.GetType().Assembly);
        }


        public string this[string key]
        {
            get
            {
                var v = this.resourceManager.GetString(key);
#if DEBUG
                if (v == null)
                    return "MISSING STRING";
#endif
                return v;
            }
        }


        public string GetEnumString(Enum value)
        {
            // ie. MyEnum.MyValue1 - will look key named - MyEnumMyValue1
            var key = value.GetType().Name + value;
            return this[key];
        }
    }
}
