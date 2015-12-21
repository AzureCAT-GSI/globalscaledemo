using GlobalDemo.DAL;
using GlobalDemo.DAL.Azure;
using GlobalDemo.Web.Models;
using Microsoft.WindowsAzure.Storage;
using System;
using System.Configuration;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace GlobalDemo.Web.Controllers
{
    [Authorize]    
    public class UploadController : ApiController
    {
        /// <summary>
        /// Gets a SAS token to add files to blob storage.
        /// The SAS token is good for 2 minutes.
        /// </summary>
        /// <returns>String for the SAS token</returns>  
        [ResponseType(typeof(StorageResponse))]
        [HttpGet]
        [Route("api/upload/{extension}")]
        public IHttpActionResult Get(string extension)
        {
            Regex rg = new Regex(@"^[a-zA-Z0-9]{1,3}$");
            if(!rg.IsMatch(extension))
            {
                throw new HttpResponseException(System.Net.HttpStatusCode.BadRequest);
            }

            string connectionString = SettingsHelper.LocalStorageConnectionString;
            var account = CloudStorageAccount.Parse(connectionString);
            StorageRepository repo = new StorageRepository(account);

            //Get the SAS token for the container.  Allow writes for 2 minutes
            var sasToken = repo.GetBlobContainerSASToken();

            //Get the blob so we can get the full path including container name
            var id = Guid.NewGuid().ToString();
            var newFileName = id + "." + extension;

            string blobURL = repo.GetBlobURI(
                newFileName, 
                DAL.Azure.StorageConfig.UserUploadBlobContainerName).ToString();


            //This function determines which storage account the blob will be
            //uploaded to, enabling the future possibility of sharding across 
            //multiple storage accounts.
            var client = account.CreateCloudBlobClient();

            var response = new StorageResponse
            {
                ID = id,
                StorageAccountName = client.BaseUri.Authority.Split('.')[0],
                BlobURL = blobURL,                
                BlobSASToken = sasToken,
                ServerFileName = newFileName
            };

            return Ok(response);
        }



        /// <summary>
        /// Notify the backend that a new file was uploaded
        /// by sending a queue message.
        /// </summary>
        /// <param name="value">The name of the blob to be processed</param>
        /// <returns>Void</returns>
        public async Task Post(CompleteRequest item)
        {
            string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            //Get the owner name field
            string ownerName = ClaimsPrincipal.Current.FindFirst("name").Value;
            //Replace any commas with periods
            ownerName = ownerName.Replace(',', '.');

            string message = string.Format(
                "{0},{1},{2},{3}, {4}, {5}, {6}", 
                item.ID, 
                item.ServerFileName, 
                item.StorageAccountName, 
                owner, 
                ownerName, 
                item.BlobURL, 
                SettingsHelper.CurrentRegion);

            //Send a queue message to the local storage account 
            //The local web job will pick it up and broadcast to 
            //all storage accounts in appSettings prefixed with "Storage"

            var repo = new StorageRepository(SettingsHelper.LocalStorageConnectionString);
            await repo.SendBroadcastQueueMessageAsync(message);
                        
        }
    }
}