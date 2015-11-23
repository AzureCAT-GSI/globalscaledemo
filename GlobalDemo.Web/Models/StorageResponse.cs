using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GlobalDemo.Web.Models
{
    public class StorageResponse
    {
        public string ID { get; set; }
        public string BlobURL { get; set; }
        public string BlobSASToken { get; set; }
        public string ServerFileName { get; set; }
        public string StorageAccountName { get; set; }
    }
}