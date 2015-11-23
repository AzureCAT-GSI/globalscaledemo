using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlobalDemo.DAL.Models
{
    public interface IPhotoModel
    {
        string ID { get; set; }
        string BlobURL { get; set; }
        string ThumbnailURL { get; set; }
        string ServerFileName { get; set; }
        string StorageAccountName { get; set; }
        string Owner { get; set; }
        string OwnerName { get; set; }
        DateTime DateAdded { get; set; }
    }
}
