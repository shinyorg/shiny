using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Shiny.IO;


namespace Shiny.Infrastructure
{
    public class XmlSerializerImpl : ISerializer
    {
        public T Deserialize<T>(string value) => (T)this.Deserialize(typeof(T), value);


        public object Deserialize(Type objectType, string value)
        {
            var serializer = new XmlSerializer(objectType);
            return serializer.Deserialize(value.ToStream());
        }


        public string Serialize(object value)
        {
            var serializer = new XmlSerializer(value.GetType());
            using (var ms = new MemoryStream())
            {
                serializer.Serialize(ms, value);
                var s = Encoding.UTF8.GetString(ms.ToArray());
                return s;
            }
        }
    }
}
