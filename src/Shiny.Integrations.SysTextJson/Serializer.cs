using System;
using Shiny.Infrastructure;


namespace Shiny
{
    public class Serializer : ISerializer
    {
        public T Deserialize<T>(string value)
        {
            throw new NotImplementedException();
        }

        public object Deserialize(Type objectType, string value)
        {
            throw new NotImplementedException();
        }

        public string Serialize(object value)
        {
            throw new NotImplementedException();
        }
    }
}
