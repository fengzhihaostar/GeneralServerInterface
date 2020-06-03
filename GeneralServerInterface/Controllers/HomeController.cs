using GeneralServerInterface.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Linq;
using ServiceStack;
using System.Threading.Tasks;
using System.Web.Http;

namespace GeneralServerInterface.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return View();
        }
        public async Task<Object> MqttResPonse(string requestId)
        {
            var count = 0;
            return await Task.Run(() =>
            {
                RedisHelper<string> redis = new RedisHelper<string>();
                while (true)
                {
                    if (count < 600)
                    {
                        Thread.Sleep(100);
                        var result = redis.Get(requestId);
                        if (result != null)
                        {
                            redis.Remove(requestId);
                            var data = JObject.Parse(result);
                            return (Object)data;
                            //return Json(result, JsonRequestBehavior.AllowGet);
                            //break;
                        }

                        count++;
                    }
                    else
                    {
                        return "";
                    }
                }
                //return await MqttResPonse(requestId);
            });
        }
    }
}
