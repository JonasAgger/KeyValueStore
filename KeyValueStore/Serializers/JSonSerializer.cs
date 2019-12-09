using Newtonsoft.Json;
using System.Text;

namespace KeyValueStore.Serializers
{
    public class JSonSerializer : ISerializer
    {
        public T Deserialize<T>(byte[] bytes)
        {
            var stringData = Encoding.UTF8.GetString(bytes);
            return JsonConvert.DeserializeObject<T>(stringData);
        }

        public byte[] Serialize<T>(T value)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(value));
        }
    }
}
