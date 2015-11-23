using System;
using System.Runtime.Serialization;

namespace GlobalDemo.DAL.Models
{
    //Must have SerializableAttribute for Redis cache.
    //Must have DataContract attribute to prevent against
    //backingfield being serialized into output.
    //http://stackoverflow.com/questions/12334382/net-webapi-serialization-k-backingfield-nastiness
    [Serializable]
    [DataContract]
    public class PhotoModel : IPhotoModel
    {
        public PhotoModel() { }
        public PhotoModel(PhotoEntity entity)
        {
            this.ID = entity.ID;
            
            this.StorageAccountName = entity.StorageAccountName;
            this.ServerFileName = entity.ServerFileName;
            this.Owner = entity.Owner;
            this.OwnerName = entity.OwnerName;
            this.BlobURL = entity.BlobURL;
            this.ThumbnailURL = entity.ThumbnailURL;
            this.DateAdded = entity.DateAdded;
        }
        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public string ServerFileName { get; set; }
        [DataMember]
        public string StorageAccountName { get; set; }
        [DataMember]
        public string Owner { get; set; }
        [DataMember]
        public string OwnerName { get; set; }
        [DataMember]
        public string BlobURL { get; set; }
        [DataMember]
        public string ThumbnailURL { get; set; }
        [DataMember]
        public DateTime DateAdded { get; set; }
    }
}
