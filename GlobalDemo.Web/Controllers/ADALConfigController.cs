using GlobalDemo.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace GlobalDemo.Web.Controllers
{
    public class ADALConfigController : ApiController
    {
        [ResponseType(typeof(ADALConfigResponse))]
        public IHttpActionResult Get()
        {
            return Ok(new ADALConfigResponse());
        }
    }
}
