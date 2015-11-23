using GlobalDemo.DAL;
using GlobalDemo.DAL.Azure;
using GlobalDemo.DAL.Models;
using GlobalDemo.DAL.Redis;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlobalDemo.WebJob
{
    internal static class Helpers
    {
        internal static async Task ReplicateBlobAsync(PhotoModel model, TextWriter log)
        {
            //The source connection string needs to be in AppSettings using the
            //storage account name.
            var sourceConnectionString = ConfigurationManager.AppSettings[model.StorageAccountName];

            //The target connection string is the local region's storage account
            var targetConnectionString = SettingsHelper.LocalStorageConnectionString;

            //Copy from the upload container to the photos container,
            //    potentially across storage accounts
            await log.WriteLineAsync("sourceConnectionString: " + sourceConnectionString);
            await log.WriteLineAsync("targetConnectionString: " + targetConnectionString);

            var storageRepo = new StorageRepository(sourceConnectionString);
            var container = await storageRepo.ReplicateBlobAsync(
                targetConnectionString,
                StorageConfig.UserUploadBlobContainerName,
                StorageConfig.PhotosBlobContainerName,
                model.ServerFileName, log);

            //Monitor the copy operation and wait for it to finish
            //before proceeding
            await storageRepo.MonitorCopy(container, model.ServerFileName, log);
        }
        internal static async Task SaveToTableStorageAsync(PhotoModel p, TextWriter log)
        {
            var storageConnectionString = SettingsHelper.LocalStorageConnectionString;
            var repo = new StorageRepository(storageConnectionString);
            var result = await repo.SaveToTableStorageAsync(DAL.Azure.StorageConfig.TableName, p);

            await log.WriteLineAsync("Save to table HTTP result: " + result);
        }

        internal static async Task SaveToRedisAsync(PhotoModel p, TextWriter log)
        {
            //Use repository to add to all caches
            IDatabase cache = RedisCache.Connection.GetDatabase();
            RedisRepository repo = new RedisRepository(cache);
            await repo.AddPhotoToCachesAsync(p);

        }


        internal static async Task CreateThumbnailAsync(StorageRepository repo, string fileName, TextWriter log)
        {
            using (var memStream = await repo.GetBlob(StorageConfig.PhotosBlobContainerName, fileName))
            {
                MemoryStream thumbnail = null;
                try
                {
                    thumbnail = PhotoEditor.ProcessImage(memStream);
                    await repo.UploadBlobAsync(thumbnail, StorageConfig.ThumbnailsBlobContainerName, fileName);
                }
                catch (Exception oops)
                {
                    await log.WriteAsync(oops.Message);
                    throw oops;
                }
                finally
                {
                    if (null != thumbnail)
                    {
                        thumbnail.Dispose();
                    }
                }

            }
        }
    }
}
