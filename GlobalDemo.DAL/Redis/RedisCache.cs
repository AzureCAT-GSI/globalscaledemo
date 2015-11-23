using Microsoft.Azure;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace GlobalDemo.DAL.Redis
{
    public static class RedisCache
    {
        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            // Replace these values with the values from your Azure Redis Cache instance.
            // For more information, see http://aka.ms/ConnectToTheAzureRedisCache
                        
            return ConnectionMultiplexer.Connect(SettingsHelper.RedisCacheConnectionString);
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }
        public static async Task<T> GetAsync<T>(this IDatabase cache, string key)
        {
            var item = await cache.StringGetAsync(key);
            return Deserialize<T>(item);
        }

        public static async Task SetAsync(this IDatabase cache, string key, object value)
        {
            await cache.StringSetAsync(key, Serialize(value));
        }

        public static async Task SetAsync<T>(this IDatabase cache, string key, T value)
        {
            await cache.StringSetAsync(key, Serialize(value));
        }

        public static async Task<IEnumerable<T>> SetMembersAsync<T>(this IDatabase cache, string key)
        {
            var ret = new List<T>();
            var result = await cache.SetMembersAsync(key);
            foreach (var item in result)
            {
                ret.Add(Deserialize<T>(item));
            }

            return ret;
        }

        public static async Task SetAddAsync<T>(this IDatabase cache, string key, T value)
        {
            await cache.SetAddAsync(key, Serialize(value));
        }


        public static async Task<IEnumerable<T>> ListRangeAsync<T>(this IDatabase cache, string key, long startIndex = 0, long stopIndex = -1)
        {
            List<T> ret = new List<T>();
            var result = await cache.ListRangeAsync(key, startIndex, stopIndex);
            foreach (var item in result)
            {
                ret.Add(Deserialize<T>(item));
            }
            return ret;
        }

        public static async Task ListLeftPushAsync<T>(this IDatabase cache, string key, T value)
        {
            await cache.ListLeftPushAsync(key, Serialize(value));
        }


        public static async Task ClearCacheForKeyAsync(this IDatabase cache, string key)
        {
            await cache.KeyDeleteAsync(key);
        }


        static byte[] Serialize(object o)
        {
            if (o == null)
            {
                return null;
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream())
            {
                binaryFormatter.Serialize(memoryStream, o);
                byte[] objectDataAsStream = memoryStream.ToArray();
                return objectDataAsStream;
            }
        }

        static T Deserialize<T>(byte[] stream)
        {
            if (stream == null)
            {
                return default(T);
            }

            BinaryFormatter binaryFormatter = new BinaryFormatter();
            using (MemoryStream memoryStream = new MemoryStream(stream))
            {
                T result = (T)binaryFormatter.Deserialize(memoryStream);
                return result;
            }
        }
    }
}
