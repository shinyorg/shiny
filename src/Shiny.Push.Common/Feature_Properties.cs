using System.Collections.Generic;


namespace Shiny.Push
{
    public static class FeatureProperties
    {
        public static IReadOnlyDictionary<string, string>? TryGetProperties(this IPushManager push)
            => (push as IPushPropertySupport)?.CurrentProperties;


        public static void TrySetProperties(this IPushManager push, params (string Property, string Value)[] properties)
        {
            if (push is IPushPropertySupport props)
            {
                props.ClearProperties();
                foreach (var prop in properties)
                    props.SetProperty(prop.Property, prop.Value);
            }
        }


        public static bool IsPropertiesSupported(this IPushManager push)
            => push is IPushPropertySupport;


        public static void TryClearProperties(this IPushManager push)
            => (push as IPushPropertySupport)?.ClearProperties();


        public static void TrySetProperty(this IPushManager push, string property, string value)
            => (push as IPushPropertySupport)?.SetProperty(property, value);
    }
}
