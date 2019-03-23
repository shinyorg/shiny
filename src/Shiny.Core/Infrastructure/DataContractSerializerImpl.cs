//using System;
//using System.IO;
//using System.Runtime.Serialization;
//using System.Text;

//namespace Acr.Infrastructure
//{
//    public class DataContractSerializerImpl : ISerializer
//    {
//        public T Deserialize<T>(string value) => (T)this.Deserialize(typeof(T), value);


//        public object Deserialize(Type objectType, string value)
//        {
//            var dcs = new DataContractSerializer(objectType);
//            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(value)))
//                return dcs.ReadObject(ms);
//        }


//        public string Serialize(object value)
//        {
//            var dcs = new DataContractSerializer(value.GetType());
//            using (var ms = new MemoryStream())
//            {
//                dcs.WriteObject(ms, value);
//                var s = Encoding.UTF8.GetString(ms.ToArray());
//                return s;
//            }
//        }
//    }
//}
