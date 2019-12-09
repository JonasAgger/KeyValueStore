using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using KeyValueStore.Serializers;

namespace KeyValueStore
{
    public class KeyValueStore : IKeyValueStore
    {
        private const string DbName = "KeyValue.db";
        private const string DbIndexName = "KeyValue.dbindex";
        private readonly FileStream dbFileStream;
        private readonly FileStream indexFileStream;
        private readonly bool usingCustomSerializer;
        private readonly ISerializer serializer = new JSonSerializer();
        private readonly string tempPath;

        public KeyValueStore(string databasePath = null, bool shouldFlush = false, bool shouldUseTemp = false, ISerializer serializer = null)
        {
            if (string.IsNullOrEmpty(databasePath)) databasePath = "./";
            if (shouldUseTemp) tempPath = databasePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

            this.dbFileStream = new FileStream($"{databasePath}{DbName}", shouldFlush ? FileMode.Create : FileMode.OpenOrCreate);
            this.indexFileStream = new FileStream($"{databasePath}{DbIndexName}", shouldFlush ? FileMode.Create : FileMode.OpenOrCreate);

            if (serializer != null)
            {
                usingCustomSerializer = true;
                this.serializer = serializer;
            }
        }

        ~KeyValueStore()
        {
			dbFileStream?.Dispose();
            indexFileStream?.Dispose();
			
            if (string.IsNullOrEmpty(tempPath)) return;
            
            File.Delete($"{tempPath}{DbName}");
            File.Delete($"{tempPath}{DbIndexName}");
        }

        public void Store<T>(string identifier, T value) => StoreAsync(identifier, value).Wait();
        public async Task StoreAsync<T>(string identifier, T value)
        {
            bool shouldUpdate = await FindKey(identifier) != null;
            
            var (startIndex, endIndex) = await WriteData(value);
            var key = new Key(identifier, startIndex, endIndex);
            await WriteKey(key, shouldUpdate);
        }

        public T Fetch<T>(string identifier) => FetchAsync<T>(identifier).Result;
        public async Task<T> FetchAsync<T>(string identifier)
        {
            var key = await FindKey(identifier);
            if (key == null) return default(T);

            var rawData = new byte[key.DataLength];
            dbFileStream.Position = key.StartIndex;
            var bytesRead = await dbFileStream.ReadAsync(rawData, 0, key.DataLength);

            return bytesRead == key.DataLength ? ByteArrayToObj<T>(rawData) : default(T);
        }

        public bool Delete(string identifier) => DeleteAsync(identifier).Result;
        public async Task<bool> DeleteAsync(string identifier)
        {
            var key = await FindKey(identifier);
            if (key == null) return false;

            key.Delete();
            await WriteKey(key, true);
            return true;
        }
        


        public List<string> GetAllKeys() => GetAllKeysAsync().Result;
        public async Task<List<string>> GetAllKeysAsync()
        {
            // Reset the stream to beginning 
            indexFileStream.Position = 0;
            var bytes = new byte[Key.KeyLength];
            var keys = new List<string>();

            while (indexFileStream.Position != indexFileStream.Length)
            {
                await indexFileStream.ReadAsync(bytes, 0, Key.KeyLength);
                var key = Key.FromBytes(bytes);
                if (key != null && !key.IsDeleted)
                {
                    keys.Add(key.Identifier);
                }
            }

            return keys;
        }

        // Utility Functions

        // Wat to do wit dis. 
        private string GetMd5Hash<T>(MD5 md5Hash, byte[] data)
        {
            return string.Concat(md5Hash.ComputeHash(data).Select(x => x.ToString("x2")));
        }

        private async Task WriteKey(Key key, bool shouldUpdate = false)
        {
            if (!shouldUpdate)
                indexFileStream.Position = indexFileStream.Length;

            await indexFileStream.WriteAsync(key.GetBytes, 0, key.GetBytes.Length);
            await indexFileStream.FlushAsync();
        }

        private async Task<Key> FindKey(string identifier)
        {
            indexFileStream.Position = 0;
            if (indexFileStream.Length < Key.KeyLength) return null;

            var bytes = new byte[Key.KeyLength];
            Key key = null; 
            var found = false;
            while (indexFileStream.Position != indexFileStream.Length)
            {
                await indexFileStream.ReadAsync(bytes, 0, Key.KeyLength);
                key = Key.FromBytes(bytes);
                if (key != null && !key.IsDeleted && key.Identifier == identifier)
                {
                    found = true;
                    indexFileStream.Position = indexFileStream.Position - Key.KeyLength;
                    break;
                }
            }

            return found ? key : null;
        }

        private async Task<(long StartIndex, long EndIndex)> WriteData<T>(T data)
        {
            var startIndex = dbFileStream.Position = (int)dbFileStream.Length;
            var dataBytes = ObjectToByteArray(data);
            await dbFileStream.WriteAsync(dataBytes, 0, dataBytes.Length);
            await dbFileStream.FlushAsync();
            var endIndex = dbFileStream.Position;
            return (startIndex, endIndex);
        }

        #region Serialization


        private byte[] ObjectToByteArray<T>(T obj)
        {
            return obj == null ? null : serializer.Serialize(obj);
        }

        private T ByteArrayToObj<T>(byte[] data)
        {
            if (data == null || data.Length == 0)
                return default(T);

            return serializer.Deserialize<T>(data);
        }

        #endregion

        private class Key
        {
            public const int KeyLength = 64;
            private const int MaxIdentifierLength = 32;
            private const char Delimiter = '_';
            private const string Deleted = "DELETED";
            private readonly byte[] bytes;

            internal string Identifier { get; set; }
            internal int StartIndex { get; set; }
            internal int EndIndex { get; set; }
            internal int DataLength => EndIndex - StartIndex;
            internal bool IsDeleted { get; set; }
            internal byte[] GetBytes => bytes;

            internal void Delete()
            {
                var stringBytes = Encoding.UTF8.GetBytes($"{Deleted}{Delimiter}{Identifier}{Delimiter}{StartIndex}{Delimiter}{EndIndex}{Delimiter}");
                for (int i = 0; i < stringBytes.Length; i++)
                {
                    bytes[i] = stringBytes[i];
                }

                bytes[KeyLength - 1] = (byte)'\n';
                IsDeleted = true;
            }

            internal Key(string identifier, int startIndex, int endIndex)
            {
                bytes = new byte[KeyLength];
                var stringBytes = Encoding.UTF8.GetBytes($"{identifier}{Delimiter}{startIndex}{Delimiter}{endIndex}{Delimiter}");
                if (stringBytes.Length > MaxIdentifierLength) throw new ArgumentException($"Identifer is too long. Max length is {MaxIdentifierLength} bytes");

                for (int i = 0; i < stringBytes.Length; i++)
                {
                    bytes[i] = stringBytes[i];
                }

                bytes[KeyLength-1] = (byte)'\n';
            }

            internal Key(string identifier, long startIndex, long endIndex)
            {
                bytes = new byte[KeyLength];
                var stringBytes = Encoding.UTF8.GetBytes($"{identifier}{Delimiter}{startIndex}{Delimiter}{endIndex}{Delimiter}");
                if (stringBytes.Length > MaxIdentifierLength) throw new ArgumentException($"Identifer is too long. Max length is {MaxIdentifierLength} bytes");

                for (int i = 0; i < stringBytes.Length; i++)
                {
                    bytes[i] = stringBytes[i];
                }

                bytes[KeyLength - 1] = (byte)'\n';
            }

            private Key(byte[] readBytes)
            {
                bytes = readBytes;
                var decodedString = Encoding.UTF8.GetString(readBytes).Split(new []{Delimiter}, StringSplitOptions.RemoveEmptyEntries);
                if (decodedString[0] == Deleted) IsDeleted = true;
                else
                {
                    Identifier = decodedString[0];
                    StartIndex = int.Parse(decodedString[1]);
                    EndIndex = int.Parse(decodedString[2]);
                }
            }

            internal static Key FromBytes(byte[] bytes)
            {
                return bytes.All(x => x == 0) ? null : new Key(bytes);
            }
        }
    }
}