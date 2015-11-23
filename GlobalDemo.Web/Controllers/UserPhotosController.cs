using GlobalDemo.DAL;
using GlobalDemo.DAL.Azure;
using GlobalDemo.DAL.Models;
using GlobalDemo.DAL.Redis;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace GlobalDemo.Web.Controllers
{
    [Authorize]
    public class UserPhotosController : ApiController
    {
        /// <summary>
        /// Gets all items from the Redis cache.  
        /// Does not use output cache.
        /// </summary>
        /// <returns>List of IPhotoModel</returns>        
        [ResponseType(typeof(IEnumerable<IPhotoModel>))]
        public async Task<IHttpActionResult> Get()
        {
            var cache = RedisCache.Connection.GetDatabase();
            var repo = new RedisRepository(cache);

            string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;

            var items = await repo.GetPhotosForUserAsync(owner);
            //Do this so we can get the count
            List<IPhotoModel> typedItems = new List<IPhotoModel>(items);
            
            if(typedItems.Count == 0)
            {
                //Nothing in cache... head off to storage.
                var storageRepo = new StorageRepository(SettingsHelper.LocalStorageConnectionString);
                
                var photos = await storageRepo.GetPhotosFromTableAsync( DAL.Azure.StorageConfig.TableName, owner);
                foreach (var photo in photos)
                {
                    //TODO: Find a MUCH better algorithm than
                    //      iterating every item and calling
                    //      Redis 3 times in a row for each
                    //      item.  This is PAINFUL.
                    await repo.AddPhotoToCachesAsync(photo);
                }
                items = photos;                
            }
            return Ok(items);
        }

        /// <summary>
        /// Gets a single item from the cache based on its ID.
        /// Does not use output cache. 
        /// </summary>
        /// <param name="id">ID of the photo</param>
        /// <returns>IPhotoModel</returns>        
        [ResponseType(typeof(IPhotoModel))]
        public async Task<IHttpActionResult> GetPhotoModel(string id)
        {
            var cache = RedisCache.Connection.GetDatabase();
            var repo = new RedisRepository(cache);

            //Get a single item from the cache based on its ID
            var photo = await repo.GetPhotoByIDAsync(id);

            if (null == photo)
            {
                //Not in the cache.  Retrieve from storage.
                string connectionString = SettingsHelper.LocalStorageConnectionString;
                string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
                var storageRepo = new StorageRepository(connectionString);
                photo = await storageRepo.GetPhotoFromTableAsync( DAL.Azure.StorageConfig.PhotosBlobContainerName, owner, id);
                if (null == photo)
                {
                    //Not found in cache or storage.                      
                }
                else
                {                    
                    //Update the cache using the cache aside pattern.
                    await repo.AddPhotoToUserCacheAsync(photo);
                }
            }
            if (null != photo)
            {
                return Ok(photo);
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Clears the entire user cache
        /// </summary>
        /// <returns>200 OK</returns>
        public async Task<IHttpActionResult> Delete()
        {
            var cache = RedisCache.Connection.GetDatabase();
            var repo = new RedisRepository(cache);

            await repo.ClearAllUsersCacheAsync();
            
            return Ok();
        }

        /// <summary>
        /// Clears the user cache
        /// </summary>
        /// <returns>200 OK</returns>
        public async Task<IHttpActionResult> Delete(string id)
        {
            var cache = RedisCache.Connection.GetDatabase();
            var repo = new RedisRepository(cache);
            string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            await repo.ClearUserCacheAsync(owner);
            return Ok();
        }
    }
}
