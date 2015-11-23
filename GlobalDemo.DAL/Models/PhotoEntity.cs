using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlobalDemo.DAL.Models
{
    [Serializable]
    public class PhotoEntity : TableEntity, IPhotoModel
    {
        public PhotoEntity()  { }

        public PhotoEntity(IPhotoModel model)
        {
            this.PartitionKey = model.Owner;
            this.RowKey =model.ID;
                       
            this.StorageAccountName = model.StorageAccountName;
            this.ServerFileName = model.ServerFileName;
            this.Owner = model.Owner;
            this.OwnerName = model.OwnerName;
            this.BlobURL = model.BlobURL;
            this.ThumbnailURL = model.ThumbnailURL;
            this.DateAdded = model.DateAdded;
        }

        public string ID { get; set; }
        public string BlobURL { get; set; }
        public string StorageAccountName { get; set; }
        public string Owner { get; set; }
        public string OwnerName { get; set; }
        public string ServerFileName { get; set; }
        public DateTime DateAdded { get; set; }
        public string ThumbnailURL { get; set; }
        public int YearAdded { get; set; }
        public int MonthAdded { get; set; }
        public int DayAdded { get; set; }
    }
}
