using System;
using System.Reflection;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.Hosting.Hubs.Infrastructure
{
    public class HostingHubRegistrationImpl : IHostingHubRegistration
    {
        public void Register(IBleHostingManager manager, Type[] hubTypes)
        {
            foreach (var hub in hubTypes)
            {
                var serviceAttribute = hub
                    .GetType()
                    .GetCustomAttribute<ServiceAttribute>();

                if (serviceAttribute == null)
                    throw new ArgumentException("");


                var methods = hub
                    .GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance);

                foreach (var method in methods)
                {
                    var characteristicAttribute = method.GetCustomAttribute<CharacteristicAttribute>();
                    if (characteristicAttribute != null)
                    {

                    }
                }
            }
        }


        static IShinyBleHub GetHub(IServiceProvider services, Type hubType)
        {
            // TODO: resolve the hubcontext to init later on for notification
            var hub = (IShinyBleHub)services.ResolveOrInstantiate(hubType);

            return hub;
        }


        static CharacteristicProperties GetCharacteristicProperties(MethodInfo method)
        {
            CharacteristicProperties props = 0;
            if (method.ReturnType != typeof(Task))
                throw new ArgumentException("Only async operations are permitted");

            if (method.ReturnType == typeof(Task<>))
                props |= CharacteristicProperties.Read;

            var parms = method.GetParameters();
            if (parms.Length > 0)
                props |= CharacteristicProperties.Write;
            //TODO: CharacteristicProperties.WriteWithoutResponse

            return props;
        }
    }
}
