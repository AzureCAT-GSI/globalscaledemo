using GlobalDemo.DAL;
using GlobalDemo.DAL.Azure;
using GlobalDemo.DAL.Models;
using Microsoft.Azure.WebJobs;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GlobalDemo.WebJob
{
    public class Functions
    {



        // This function will get triggered/executed when a new message is written 
        // on an Azure Queue called queue.
        public static async Task ProcessQueueMessage(
            [QueueTrigger("uploadqueue")] string message,
            TextWriter log)
        {
            log.WriteLineAsync(message).Wait();

            var m = message.Split(',');
            await log.WriteAsync("Message received: " + m);

            var model = new PhotoModel
            {
                ID = m[0],
                ServerFileName = m[1],
                StorageAccountName = m[2],
                Owner = m[3],
                OwnerName = m[4],
                BlobURL = m[5]
            };

            //Copy blob from source to destination
            await log.WriteAsync("Replicating blob...");
            await Helpers.ReplicateBlobAsync(model, log);

            try
            {


                //Change the blob URL to point to the new location!    
                await log.WriteAsync("Getting blob URIs");
                string storageConnectionString = SettingsHelper.LocalStorageConnectionString;
                var repo = new StorageRepository(storageConnectionString);
                model.BlobURL = repo.GetBlobURI(model.ServerFileName, StorageConfig.PhotosBlobContainerName).ToString();
                model.ThumbnailURL = repo.GetBlobURI(model.ServerFileName, StorageConfig.ThumbnailsBlobContainerName).ToString();

                //Create thumbnail
                await log.WriteAsync("Creating thumbnail");
                await Helpers.CreateThumbnailAsync(repo, model.ServerFileName, log);

                //Store in table storage
                await log.WriteAsync("Saving to table storage");
                await Helpers.SaveToTableStorageAsync(model, log);

                //Add to Redis cache
                await log.WriteAsync("Saving to Redis");
                await Helpers.SaveToRedisAsync(model, log);
            }
            catch (Exception oops)
            {
                await log.WriteLineAsync(oops.Message);
            }
        }







    }
}
