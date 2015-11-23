using GlobalDemo.DAL;
using GlobalDemo.DAL.Azure;
using GlobalDemo.DAL.Models;
using GlobalDemo.DAL.Redis;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using WebApi.OutputCache.V2;

namespace GlobalDemo.Web.Controllers
{
    public class PhotoController : ApiController
    {
        /// <summary>
        /// Gets all items from the cache.  Uses output cache
        /// to cache result for 10 seconds
        /// </summary>
        /// <returns>List of IPhotoModel</returns>
        [CacheOutput(ClientTimeSpan = 10, ServerTimeSpan = 10)]
        [ResponseType(typeof(PhotoModel))]
        public async Task<IHttpActionResult> Get()
        {
            var cache = RedisCache.Connection.GetDatabase();
            var repo = new RedisRepository(cache);
            var items = await repo.GetAllPhotosAsync();
            
            List<IPhotoModel> typedItems = new List<IPhotoModel>(items);
            if(typedItems.Count == 0)
            {
                //Pull from storage.  This is a cross-partition query,
                //  and will be slower than using Redis.
                var storageConnectionString = SettingsHelper.LocalStorageConnectionString;
                var storageRepo = new StorageRepository(storageConnectionString);
                typedItems = await storageRepo.GetLatestFromTableStorageAsync();
                if(typedItems.Count > 0)
                {
                    foreach (var item in typedItems)
                    {
                        //Add to cache as cache-aside pattern
                        await repo.AddPhotoToAllUsersCacheAsync(item);
                    }
                    items = typedItems;
                }
            }


            return Ok(items);
        }

        /// <summary>
        /// Gets a single item from the cache based on its ID.
        /// Uses output cache to cache result for 10 seconds.       
        /// </summary>
        /// <param name="id">ID of the photo</param>
        /// <returns>IPhotoModel</returns>
        [CacheOutput(ClientTimeSpan = 10, ServerTimeSpan = 10)]
        [ResponseType(typeof(PhotoModel))]
        public async Task<IHttpActionResult> GetPhotoModel(string id)
        {            
            var cache = RedisCache.Connection.GetDatabase();
            var repo = new RedisRepository(cache);
            
            //Get a single item from the cache based on its ID
            var photo = await repo.GetPhotoByIDAsync(id);

            if(null == photo)
            {
                //Not in the cache.  Try to retrieve based on 
                //  current user.  This won't work if it's another user's photo.
                if(User.Identity.IsAuthenticated)
                {
                    string connectionString = SettingsHelper.LocalStorageConnectionString;
                    string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
                    var storageRepo = new StorageRepository(connectionString);
                    photo = await storageRepo.GetPhotoFromTableAsync(DAL.Azure.StorageConfig.PhotosBlobContainerName, owner, id);
                    if (null == photo)
                    {
                        //Not found in cache or storage.                      
                    }
                    else
                    {                        
                        //Update the cache using the cache aside pattern.
                        await repo.AddPhotoToCachesAsync(photo);
                    }
                }
            }
            if(null != photo)
            {
                return Ok(photo);
            }
            else
            {
                return NotFound();
            }
        }
        
    }
}