using GlobalDemo.DAL.Azure;
using GlobalDemo.DAL.Models;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlobalDemo.DAL.Redis
{
    public class RedisRepository
    {
        private IDatabase _cache = null;

        public RedisRepository(IDatabase cache)
        {
            _cache = cache;    
        }

        public async Task<IEnumerable<IPhotoModel>> GetAllPhotosAsync()
        {
            return await RedisCache.ListRangeAsync<IPhotoModel>(_cache, RedisConfig.AllUsersCache);
            
        }

        public async Task<IEnumerable<IPhotoModel>> GetPhotosForUserAsync(string user)
        {
            return await RedisCache.ListRangeAsync<IPhotoModel>(_cache, RedisConfig.PhotosForUserCache(user));
        }

        public async Task<IPhotoModel> GetPhotoByIDAsync(string id)
        {
            return await RedisCache.GetAsync<IPhotoModel>(_cache, RedisConfig.PhotoByIDCache(id));
        }

        public async Task AddPhotoToCachesAsync(IPhotoModel photo)
        {
            //Add to list of all user photos
            await RedisCache.ListLeftPushAsync<IPhotoModel>(_cache, RedisConfig.AllUsersCache, photo);
            //Add to list of user-specific photos
            await RedisCache.ListLeftPushAsync<IPhotoModel>(_cache, RedisConfig.PhotosForUserCache(photo.Owner), photo);
            //Add single item to cache
            await RedisCache.SetAsync<IPhotoModel>(_cache, RedisConfig.PhotoByIDCache(photo.ID), photo);
        }

        public async Task AddPhotoToUserCacheAsync(IPhotoModel photo)
        {
            //Add to list of user-specific photos
            await RedisCache.ListLeftPushAsync<IPhotoModel>(_cache, RedisConfig.PhotosForUserCache(photo.Owner), photo);
        }

        public async Task AddPhotoToAllUsersCacheAsync(IPhotoModel photo)
        {
            //Add to list of user-specific photos
            await RedisCache.ListLeftPushAsync<IPhotoModel>(_cache, RedisConfig.AllUsersCache, photo);
        }

        public async Task ClearUserCacheAsync(string user)
        {
            await RedisCache.ClearCacheForKeyAsync(_cache,RedisConfig.PhotosForUserCache(user));
        }

        public async Task ClearAllUsersCacheAsync()
        {
            await RedisCache.ClearCacheForKeyAsync(_cache, RedisConfig.AllUsersCache);
        }

        public async Task ClearCacheItemAsync(string id)
        {
            await RedisCache.ClearCacheForKeyAsync(_cache, RedisConfig.PhotoByIDCache(id));
        }

    }
}
