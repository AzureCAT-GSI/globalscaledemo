using GlobalDemo.DAL.Models;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GlobalDemo.DAL.Azure
{
    public class StorageRepository
    {
        CloudStorageAccount _account = null;

        public StorageRepository(CloudStorageAccount account)
        {
            _account = account;
        }

        public StorageRepository(string connectionString)
        {
            _account = CloudStorageAccount.Parse(connectionString);
        }

        /// <summary>
        /// Get a block blob's URI
        /// </summary>
        /// <param name="fileName">The file name in storage</param>
        /// <param name="containerName">The container name</param>
        /// <returns>System.Uri</returns>
        public Uri GetBlobURI(string fileName, string containerName)
        {            
            var client = _account.CreateCloudBlobClient();
            var container = client.GetContainerReference(containerName);
            var blob = container.GetBlockBlobReference(fileName);
            return blob.StorageUri.PrimaryUri;
        }

        /// <summary>
        /// Gets a blob container's SAS token
        /// </summary>
        /// <param name="containerName">The container name</param>
        /// <param name="permissions">The permissions</param>
        /// <param name="minutes">Number of minutes the permissions are effective</param>
        /// <returns>System.String - The SAS token</returns>
        public string GetBlobContainerSASToken(            
            string containerName,
            SharedAccessBlobPermissions permissions,
            int minutes)
        {

            var client = _account.CreateCloudBlobClient();

            var policy = new SharedAccessBlobPolicy();

            policy.Permissions = permissions;
            policy.SharedAccessStartTime = System.DateTime.UtcNow.AddMinutes(-10);
            policy.SharedAccessExpiryTime = System.DateTime.UtcNow.AddMinutes(10);

            var container = client.GetContainerReference(containerName);

            //Get the SAS token for the container.
            var sasToken = container.GetSharedAccessSignature(policy);

            return sasToken;
        }

        /// <summary>
        /// Gets the blob container's SAS token without any parameters.
        /// Defaults are Write permissions for 2 minutes
        /// </summary>
        /// <returns>System.String - the SAS token</returns>
        public string GetBlobContainerSASToken()
        {
            return GetBlobContainerSASToken(                
                DAL.Azure.StorageConfig.UserUploadBlobContainerName,
                SharedAccessBlobPermissions.Write,
                2);
        }


        /// <summary>
        /// Saves an entity to Azure table storage
        /// </summary>
        /// <param name="tableName">The name of the table to save to</param>
        /// <param name="model">The PhotoModel object to be saved</param>
        /// <returns>System.String - the HTTP status code of the save operation</returns>
        public async Task<string> SaveToTableStorageAsync(            
            string tableName,
            PhotoModel model)
        {
            //We use the DateAdded field for cross-partition queries
            DateTime now = System.DateTime.Now;

            model.DateAdded = now;

            PhotoEntity entity = new PhotoEntity(model);
            
            //These properties are used in a full table scan to populate
            //all photos for all users.  Needed as some way to get the 
            //items for a single day for all users.
            entity.DayAdded = now.Day;
            entity.MonthAdded = now.Month;
            entity.YearAdded = now.Year;
            
            var client = _account.CreateCloudTableClient();
            var table = client.GetTableReference(tableName);
            var operation = TableOperation.InsertOrReplace(entity);

            var result = await table.ExecuteAsync(operation);

            //TODO:  Do we need to check the HTTP status code here?
            return result.HttpStatusCode.ToString();
        }


        /// <summary>
        /// This is the most efficient way to retrieve, using 
        /// both the partition key and row key
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="tableName"></param>
        /// <param name="partitionKey"></param>
        /// <param name="rowKey"></param>
        /// <returns></returns>
        public async Task<PhotoModel> GetPhotoFromTableAsync(            
            string tableName,
            string partitionKey,
            string rowKey)
        {            
            var client = _account.CreateCloudTableClient();
            var table = client.GetTableReference(tableName);

            var operation = TableOperation.Retrieve<PhotoEntity>(partitionKey, rowKey);
            var result = await table.ExecuteAsync(operation);
            var entity = result.Result as PhotoEntity;
            return new PhotoModel(entity);
        }

        /// <summary>
        /// This is a partition scan.  Less optimal, but not as bad
        /// as a full table scan.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="tableName"></param>
        /// <param name="partitionKey"></param>
        /// <returns></returns>
        public async Task<IEnumerable<PhotoModel>> GetPhotosFromTableAsync(            
            string tableName,
            string partitionKey)
        {
            
            var client = _account.CreateCloudTableClient();
            var table = client.GetTableReference(tableName);

            var ret = new List<PhotoModel>();

            TableQuery<PhotoEntity> partitionScanQuery = new TableQuery<PhotoEntity>().Where
                (TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, partitionKey));

            TableContinuationToken token = null;
            // Page through the results
            do
            {
                TableQuerySegment<PhotoEntity> segment = await table.ExecuteQuerySegmentedAsync(partitionScanQuery, token);
                token = segment.ContinuationToken;
                foreach (PhotoEntity entity in segment)
                {
                    ret.Add(new PhotoModel(entity));
                }
            }
            while (token != null);

            return ret;
        }


        /// <summary>
        /// This is a full table scan.  Yuck.
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public async Task<List<IPhotoModel>> GetLatestFromTableStorageAsync()
        {            
            var client = _account.CreateCloudTableClient();
            var table = client.GetTableReference(StorageConfig.TableName);

            //Scan all partitions looking for entities with a DateAdded property
            //   equal to today.  This is inefficient, and one of the reasons
            //   we introduce Redis as a cache.
            TableQuery<PhotoEntity> tableScanQuery = new TableQuery<PhotoEntity>().Where
                (TableQuery.CombineFilters(
                    TableQuery.GenerateFilterCondition("YearAdded", QueryComparisons.Equal, System.DateTime.Now.Year.ToString()),
                    TableOperators.And,
                    TableQuery.CombineFilters(
                        TableQuery.GenerateFilterCondition("MonthAdded", QueryComparisons.Equal, System.DateTime.Now.Month.ToString()),
                        TableOperators.And,
                        TableQuery.GenerateFilterCondition("DayAdded", QueryComparisons.Equal, System.DateTime.Now.Day.ToString()))));

            TableContinuationToken token = null;
            List<IPhotoModel> ret = new List<IPhotoModel>();
            // Page through the results
            do
            {
                TableQuerySegment<PhotoEntity> segment = await table.ExecuteQuerySegmentedAsync(
                    tableScanQuery,
                    token);
                token = segment.ContinuationToken;
                foreach (PhotoEntity entity in segment)
                {
                    ret.Add(new PhotoModel(entity));
                }
            }
            while (token != null);
            return ret;
        }


        /// <summary>
        /// Sends a queue message
        /// </summary>
        /// <param name="data">The message to send</param>
        /// <returns></returns>
        public async Task SendQueueMessageAsync(string data)
        {            
            var client = _account.CreateCloudQueueClient();
            //Send to each region's queue
            var queue = client.GetQueueReference(DAL.Azure.StorageConfig.QueueName);

            var message = new CloudQueueMessage(data);

            await queue.AddMessageAsync(message);
        }


        /// <summary>
        /// Replicates a blob from the current storage account
        /// to a target storage account
        /// </summary>
        /// <param name="targetConnectionString">Connection string of the target storage account</param>
        /// <param name="sourceContainerName">The source container</param>
        /// <param name="targetContainerName">The target container</param>
        /// <param name="blobName">The blob to replicate</param>
        /// <param name="log">TextWriter used for logging</param>
        /// <returns>The target CloudBlobContainer, used for checking status</returns>
        public async Task<CloudBlobContainer> ReplicateBlobAsync(           
           string targetConnectionString,
           string sourceContainerName,
           string targetContainerName,
           string blobName,
           TextWriter log)
        {
            var taskId = string.Empty;

            //Create a connection to where the blob currently lives            
            CloudBlobClient sourceBlobClient = _account.CreateCloudBlobClient();

            //Create remote client
            var targetAccount = CloudStorageAccount.Parse(
                targetConnectionString);
            CloudBlobClient targetBlobClient = targetAccount.CreateCloudBlobClient();

            var sourceContainer = sourceBlobClient.GetContainerReference(sourceContainerName);
            var targetContainer = targetBlobClient.GetContainerReference(targetContainerName);

            bool created = await targetContainer.CreateIfNotExistsAsync();
            if (created)
            {
                var perms = await sourceContainer.GetPermissionsAsync();
                await targetContainer.SetPermissionsAsync(perms);
            }

            CloudBlockBlob sourceBlob = sourceContainer.GetBlockBlobReference(blobName);


            //Must use a shared access signature when copying across accounts
            //  else the target cannot read from source and you will get a 404
            //  Subtract 1 hour and add 1 hour to account for time drift between
            //  servers

            string signature = sourceBlob.GetSharedAccessSignature(
                new SharedAccessBlobPolicy
                {
                    Permissions = SharedAccessBlobPermissions.Read,
                    SharedAccessStartTime = System.DateTime.Now.AddHours(-1),
                    SharedAccessExpiryTime = System.DateTime.Now.AddHours(1)
                });

            var sourceUri = new Uri(sourceBlob.Uri.AbsoluteUri + signature);

            CloudBlockBlob targetBlob = targetContainer.GetBlockBlobReference(blobName);

            try
            {
                //Set a retry policy to try again in 10 seconds, 3 max attempts.
                var retryPolicy = new LinearRetry(new TimeSpan(0, 0, 10), 3);
                var options = new BlobRequestOptions { RetryPolicy = retryPolicy };
                
                //The StartCopy method uses spare bandwidth, there is
                //no SLA on how fast this will be copied.
                taskId = targetBlob.StartCopy(sourceUri, options: options);

            }
            catch (Exception oops)
            {
                await log.WriteLineAsync(oops.Message);
            }

            return targetContainer;
        }


        /// <summary>
        /// Monitors a blob copy operation
        /// </summary>
        /// <param name="destContainer">The destination container to monitor</param>
        /// <param name="log">The log to write output</param>
        /// <returns></returns>
        public async Task MonitorCopy(CloudBlobContainer destContainer, string fileName, TextWriter log)
        {
            bool pendingCopy = true;
            int waitSeconds = 3;

            while (pendingCopy)
            {
                pendingCopy = false;
                //This is going to get all of the blobs in the container.                 
                var destBlobList = destContainer.ListBlobs(prefix:fileName,  useFlatBlobListing: true , blobListingDetails: BlobListingDetails.Copy);

                foreach (var dest in destBlobList)
                {
                    var destBlob = dest as CloudBlob;
                    
                    if (destBlob.CopyState != null)
                    {
                        if (destBlob.CopyState.Status == CopyStatus.Aborted ||
                            destBlob.CopyState.Status == CopyStatus.Failed)
                        {
                            // Log the copy status description for diagnostics 
                            // and restart copy
                            await log.WriteLineAsync(destBlob.CopyState.ToString());
                            pendingCopy = false;
                            //Could restart the operation here if it fails                        

                            pendingCopy = true;
                            try
                            {
                                destBlob.StartCopy(destBlob.CopyState.Source);
                            }
                            catch (Exception oops)
                            {
                                await log.WriteLineAsync(oops.Message);
                                throw oops;
                            }
                        }
                        else if (destBlob.CopyState.Status == CopyStatus.Pending)
                        {
                            // We need to continue waiting for this pending copy
                            // However, let us log copy state for diagnostics
                            await log.WriteLineAsync(destBlob.CopyState.ToString());

                            pendingCopy = true;
                        }
                    }
                    else
                    {
                        //What the hell does this mean?  Why are we getting Null for this query?!?
                        // else we completed this pending copy
                        await log.WriteLineAsync(destBlob.Name.ToString() + " is null?!?!");       
                        
                        // does this mean it is pending?  wtf?                 
                    }

                    pendingCopy = false;
                }

                //Wait number of milliseconds before trying again
                Thread.Sleep(waitSeconds * 1000);
            };
        }


        public async Task<MemoryStream> GetBlob(string containerName, string fileName)
        {
            
            var client = _account.CreateCloudBlobClient();
            var container = client.GetContainerReference(containerName);
            var blockBlob = container.GetBlockBlobReference(fileName);
            var ret = new MemoryStream();
            await blockBlob.DownloadToStreamAsync(ret);

            return ret;
        }

        public async Task UploadBlobAsync(MemoryStream stream, string containerName, string fileName)
        {
            var client = _account.CreateCloudBlobClient();
            var container = client.GetContainerReference(containerName);
            var blockBlob = container.GetBlockBlobReference(fileName);
            await blockBlob.UploadFromStreamAsync(stream);
        }
    }
}
