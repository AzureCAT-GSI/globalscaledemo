using GlobalDemo.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GlobalDemo.Web.Models
{
    public class ADALConfigResponse
    {
        public string ClientId { get { return SettingsHelper.Audience; }  }
        public string Tenant { get { return SettingsHelper.Tenant; } }
        public string Instance { get { return "https://login.microsoftonline.com/"; } }
    }
}