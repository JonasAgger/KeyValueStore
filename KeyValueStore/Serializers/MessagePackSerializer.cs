using MessagePack;

namespace KeyValueStore.Serializers
{
    public class MessagePackSerializer : ISerializer
    {
        public T Deserialize<T>(byte[] bytes) => (T)MessagePack.MessagePackSerializer.Typeless.Deserialize(bytes);
        public byte[] Serialize<T>(T value) => MessagePack.MessagePackSerializer.Typeless.Serialize(value);
    }
}
