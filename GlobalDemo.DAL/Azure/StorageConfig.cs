using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlobalDemo.DAL.Azure
{
    public class StorageConfig
    {
        public static string QueueName
        {
            get { return "uploadqueue"; }
        }

        public static string UserUploadBlobContainerName
        {
            get { return "uploads"; }
        }

        public static string PhotosBlobContainerName
        {
            get { return "photos"; }
        }

        public static string ThumbnailsBlobContainerName
        {
            get{ return "thumbnails"; }
        }
        public static string TableName
        {
            get { return "photos"; }
        }
    }
}
