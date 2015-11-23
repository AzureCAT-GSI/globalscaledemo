using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace GlobalDemo.DAL
{
    public class SettingsHelper
    {
        public static string Tenant { get { return ConfigurationManager.AppSettings["ida:Tenant"]; } }
        public static string Audience { get { return ConfigurationManager.AppSettings["ida:Audience"]; } }
        public static string LocalStorageConnectionString { get { return ConfigurationManager.AppSettings["localStorageConnectionString"]; } }
        public static string RedisCacheConnectionString { get { return ConfigurationManager.AppSettings["redisCacheConnectionString"]; } }               
        public static string AzureWebJobsDashboard { get { return ConfigurationManager.AppSettings["AzureWebJobsDashboard"]; } }
        public static string AzureWebJobsStorage { get { return ConfigurationManager.AppSettings["AzureWebJobsStorage"]; } }
    }
}