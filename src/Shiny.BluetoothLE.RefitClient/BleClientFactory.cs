using System;


namespace Shiny.BluetoothLE.RefitClient
{
    public static class BleClientFactory
    {
        public static T GetInstance<T>()
        {
            if (!typeof(T).IsInterface)
                throw new ArgumentException("T argument must be an interface");

            var className = typeof(T).Name.TrimStart('I');
            var typeName = $"{typeof(T).Namespace}.{className}, {typeof(T).Assembly.GetName().Name}";
            var actualType = Type.GetType(typeName);
            if (actualType == null)
                throw new ArgumentException("Generated type was not found");

            return (T)Activator.CreateInstance(actualType);
        }
    }
}
