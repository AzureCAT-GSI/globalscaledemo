using GlobalDemo.DAL;
using System.Configuration;
using System.Web.Http;

namespace GlobalDemo.Web
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {            
            GlobalConfiguration.Configure(WebApiConfig.Register);
            StorageConfig.Configure(SettingsHelper.LocalStorageConnectionString).Wait();
        }
    }
}
