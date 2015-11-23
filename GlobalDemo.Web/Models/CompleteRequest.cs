using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GlobalDemo.Web.Models
{
    public class CompleteRequest
    {
        public string ID { get; set; }
        public string ServerFileName { get; set; }
        public string StorageAccountName { get; set; }
        public string BlobURL { get; set; }
    }
}