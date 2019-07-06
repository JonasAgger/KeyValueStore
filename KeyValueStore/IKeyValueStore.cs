using System.Collections.Generic;
using System.Threading.Tasks;

namespace KeyValueStore
{
    public interface IKeyValueStore
    {
        // Sync
        void Store<T>(string identifier, T value);
        T Fetch<T>(string identifier);
        bool Delete(string identifier);
        List<string> GetAllKeys();

        // Async
        Task StoreAsync<T>(string identifier, T value);
        Task<T> FetchAsync<T>(string identifier);
        Task<bool> DeleteAsync(string identifier);
        Task<List<string>> GetAllKeysAsync();
    }
}