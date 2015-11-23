using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GlobalDemo.DAL.Redis
{
    public class RedisConfig
    {
        public static string AllUsersCache
        {
            get { return "allusers:photos"; }
        }

        public static string PhotosForUserCache(string user)
        {
            return string.Format("user:{0}:photos", user); 
        }

        public static string PhotoByIDCache(string id)
        {
            return string.Format("photo:{0}", id);
        }
    }
}
