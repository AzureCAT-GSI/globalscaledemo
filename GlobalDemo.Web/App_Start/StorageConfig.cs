using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GlobalDemo.Web
{
    public static class StorageConfig
    {
        /// <summary>
        /// Configures the storage account used by the application.
        /// Configures to support CORS, and creates the blob, table,
        /// and queue needed for the app if they don't already exist.
        /// </summary>
        /// <param name="localStorageConnectionString">The storage account connection string</param>
        /// <returns></returns>
        public static async Task Configure(string storageConnectionString)
        {
            
            var account = CloudStorageAccount.Parse(storageConnectionString);
            var client = account.CreateCloudBlobClient();
            var serviceProperties = client.GetServiceProperties();

            //Configure CORS
            serviceProperties.Cors = new CorsProperties();
            serviceProperties.Cors.CorsRules.Add(new CorsRule()
            {
                AllowedHeaders = new List<string>() { "*" },
                AllowedMethods = CorsHttpMethods.Put | CorsHttpMethods.Get | CorsHttpMethods.Head | CorsHttpMethods.Post,
                AllowedOrigins = new List<string>() { "*" },
                ExposedHeaders = new List<string>() { "*" },
                MaxAgeInSeconds = 3600 // 60 minutes
            });

            await client.SetServicePropertiesAsync(serviceProperties);
            
            //Create the public container if it doesn't exist as publicly readable
            var container = client.GetContainerReference(GlobalDemo.DAL.Azure.StorageConfig.PhotosBlobContainerName);
            await container.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Container, new BlobRequestOptions(), new OperationContext { LogLevel = LogLevel.Informational });

            //Create the thumbnail container if it doesn't exist as publicly readable
            container = client.GetContainerReference(GlobalDemo.DAL.Azure.StorageConfig.ThumbnailsBlobContainerName);
            await container.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Container, new BlobRequestOptions(), new OperationContext { LogLevel = LogLevel.Informational });

            //Create the private user uploads container if it doesn't exist
            container = client.GetContainerReference(GlobalDemo.DAL.Azure.StorageConfig.UserUploadBlobContainerName);
            await container.CreateIfNotExistsAsync();

            //Create the "uploadqueue" queue if it doesn't exist             
            var queueClient = account.CreateCloudQueueClient();
            var queue = queueClient.GetQueueReference(GlobalDemo.DAL.Azure.StorageConfig.QueueName);
            await queue.CreateIfNotExistsAsync();

            //Create the "broadcastqueue" queue if it doesn't exist
            var broadcastQueue = queueClient.GetQueueReference(GlobalDemo.DAL.Azure.StorageConfig.BroadcastQueueName);
            bool tester = await broadcastQueue.CreateIfNotExistsAsync();
            

            //Create the "photos" table if it doesn't exist
            var tableClient = account.CreateCloudTableClient();
            var table = tableClient.GetTableReference(GlobalDemo.DAL.Azure.StorageConfig.TableName);
            await table.CreateIfNotExistsAsync();
        }
    }
}