using GeneralServerInterface.Models;
using MQTTnet;
using MQTTnet.Core;
using MQTTnet.Core.Packets;
using MQTTnet.Core.Protocol;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using GeneralServerInterface.Redis;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Threading;
using MQTTnet.Core.Server;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Diagnostics;

namespace GeneralServerInterface
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private string topic = "gxsd";
        //静态
        private static MqttServer mqttServer = null;

        private static Dictionary<string, List<BackModel>> backFiles { get; set; }
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            backFiles = new Dictionary<string, List<BackModel>>();
            new Thread(StartMqttServer).Start();
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            try
            {
                Exception ex = Server.GetLastError();
                if (ex is HttpException && ((HttpException)ex).GetHttpCode() == 404)
                {
//                    //解密密码
//                    var privateKey = @"MIICWwIBAAKBgQCbY8sGqVfzoH0E4MGeOm17QZoqHP1hnEUJ+L6jyr+h9/asQQRa
//tSwD0lJUpNbPi6w/y4W8FqyhKFolcXwVKAAXKGu3DlImCpOWVb2JW9Uz60GkerzH
//MYi/Nm5uzgc0hbUpXyzBWU8QxZ+OkB5P4Ieq+whRycq2TizqhpYOkqE77wIDAQAB
//AoGACJ+YI82AEQgmCABFHnfVnZJ9cLrdIO6gMjZ4tfRJgD6XlOWizTnisG+anBHt
//zeTNcVjlGhQUDnmDzzImFbJ7lrbLIy238VIp3e+0rffDzd9AMcx0pt9Nx2uKHMVp
//U1SG7JFspImZvgyOoqif5RL2Xw2J67kP2J3cGNX4pzxszIECQQCh07okY/fTmXgz
//o2NpVQD1pzHXzRTEH5QclfjU0q7lJ6ZaMo94U8GfaqiVH1eE3K/JAeZ7LFtLxYr/
//Vk/qtESvAkEA9dETBpHUQHdUkq3YNT6Ni2I0XjTLawrMRC5z5TVQfq2sGfAh0DWv
//ayNtUVKcpymbi31bHoD0wbzYYRCAAWLMwQJAWmB9z78I9GL8j5JLfdMcYxVKL+R4
//GYQtWr2jJ3C2foJjVHJyT9gvBZIyrn2/ihMaFV97UgUWw72CgFG69jBRPwJAE3aD
//YCDJwnTwUFDNbqHOSTv0U4UwmgAX3kojSQGopu8PUlpuAvNNOVlrvWWiG6Yyt5+s
//SEUDnBdctoq8598vwQJAP/JdbsZ4iHaZg7zGrC42FYePQCOlvc+qslksFWHXodHS
//b/QGpk/4Wp+xG7sWei6J8WopM7hgiT5N0PlL1sa2Mg==";
//                    var pubkey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCbY8sGqVfzoH0E4MGeOm17QZoqHP1hnEUJ+L6jyr+h9/asQQRatSwD0lJUpNbPi6w/y4W8FqyhKFolcXwVKAAXKGu3DlImCpOWVb2JW9Uz60GkerzHMYi/Nm5uzgc0hbUpXyzBWU8QxZ+OkB5P4Ieq+whRycq2TizqhpYOkqE77wIDAQAB";
//                    var rsa = new RSACryptoService(privateKey, pubkey);
                    var model = new RequestModel
                    {
                        RequestId = Guid.NewGuid().ToString("N"),
                        Url = Request.Path,
                        //Paramas = HttpUtility.UrlDecode(Request.QueryString.ToString()),
                        Paramas = HttpUtility.UrlDecode(Request.QueryString.ToString()),
                        Methed = Request.HttpMethod,
                        ContentType = Request.ContentType
                    };
                    if (Request.Files.Count > 0)
                    {
                        model.FileBase64Str = new List<Base64FileInfo>();
                        for (int i = 0; i < Request.Files.Count; i++)
                        {
                            //Request.Files[i].FileName
                            byte[] bytes = new byte[Request.Files[i].ContentLength];
                            Request.Files[i].InputStream.Read(bytes, 0, bytes.Length);
                            var baseStr = Convert.ToBase64String(bytes);

                            var fileMaxLeng = 1327160;//一次文件最大限制   1279KB
                            if (Request.Files[i].ContentLength< fileMaxLeng)
                            {
                                var baseFile = new Base64FileInfo { FileBase64Str = baseStr, FileName = Request.Files[i].FileName };
                                model.FileBase64Str.Add(baseFile);

                                var requestTxt = JsonConvert.SerializeObject(model);
                                var appMsg = new MqttApplicationMessage(topic, Encoding.UTF8.GetBytes(requestTxt), MqttQualityOfServiceLevel.ExactlyOnce, false);
                                mqttServer.Publish(appMsg);
                            }
                            //大文件分割传输
                            else
                            {
                                var fileArrary = SplitByLen(baseStr, fileMaxLeng);
                                var fileId = Guid.NewGuid().ToString("N");
                                for (int j = 0; j < fileArrary.Count; j++)
                                {
                                    var newBaseStr = fileArrary[j];
                                    var baseFile = new Base64FileInfo { FileBase64Str = newBaseStr, FileName = Request.Files[i].FileName, FileId = fileId, FileIndex = j, IsLastIndex = 0 };
                                    if (j == fileArrary.Count - 1)
                                    {
                                        baseFile.IsLastIndex = j;
                                    }
                                    model = new RequestModel
                                    {
                                        RequestId = Guid.NewGuid().ToString("N"),
                                        Url = Request.Path,
                                        Paramas = HttpUtility.UrlDecode(Request.QueryString.ToString()),
                                        Methed = Request.HttpMethod,
                                        ContentType = Request.ContentType,
                                        FileBase64Str = new List<Base64FileInfo>()
                                    };
                                    model.FileBase64Str.Add(baseFile);

                                    var requestTxt = JsonConvert.SerializeObject(model);
                                    var appMsg = new MqttApplicationMessage(topic, Encoding.UTF8.GetBytes(requestTxt), MqttQualityOfServiceLevel.ExactlyOnce, false);
                                    mqttServer.Publish(appMsg);
                                }
                            }
                        }
                    }
                    else
                    {
                        byte[] byts = new byte[Request.InputStream.Length];
                        Request.InputStream.Read(byts, 0, byts.Length);
                        string req = Server.UrlDecode(Encoding.UTF8.GetString(byts).Replace("+", "%2b"));
                        if (string.IsNullOrEmpty(model.Paramas)&&!string.IsNullOrEmpty(req))
                        {
                            model.Paramas = req;
                        }

                        var requestTxt = JsonConvert.SerializeObject(model);
                        var appMsg = new MqttApplicationMessage(topic, Encoding.UTF8.GetBytes(requestTxt), MqttQualityOfServiceLevel.ExactlyOnce, false);
                        mqttServer.Publish(appMsg);
                    }
                  
                    Response.Redirect("/Home/MqttResPonse?requestId=" + model.RequestId);
                    //Response.Redirect("/Error");

                    //文件
                  
                    //var filesReadToProvider = Request.Content.ReadAsMultipartAsync();
                    //foreach (var stream in filesReadToProvider.Contents)
                    //{
                    //    var fileBytes = stream.ReadAsByteArrayAsync();
                    //    var fileStream = new FileStream(Request.Files, FileMode.Create, FileAccess.Write);
                    //    fileStream.Write(fileBytes, 0, fileBytes.Length);
                    //    fileStream.Close();
                    //}
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
                throw;
            }
            if (Request.Path == "/Home/MqttResPonse")
            {
                return;
            }
        }

        #region Mqtt
        private static void StartMqttServer()
        {
            //while (true)
            //{
                if (mqttServer == null)
                {
                    try
                    {
                        var options = new MqttServerOptions
                        {
                            ConnectionValidator = p =>
                            {
                                if (p.ClientId == "c001")
                                {
                                    if (p.Username != "u001" || p.Password != "p001")
                                    {
                                        return MqttConnectReturnCode.ConnectionRefusedBadUsernameOrPassword;
                                    }
                                }
                                return MqttConnectReturnCode.ConnectionAccepted;
                            }
                        };
                        options.DefaultEndpointOptions.Port = int.Parse(ConfigurationManager.AppSettings.Get("MqttPort"));
                        mqttServer = new MqttServerFactory().CreateMqttServer(options) as MqttServer;

                        mqttServer.ApplicationMessageReceived += MqttServer_ApplicationMessageReceived;
                        mqttServer.ClientConnected += MqttServer_ClientConnected;
                        mqttServer.ClientDisconnected += MqttServer_ClientDisconnected;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        return;
                    }
                }
                mqttServer.StartAsync();
                Thread.Sleep(int.MaxValue);
                //Console.WriteLine("MQTT服务启动成功！");
            //}
        }
        private static void MqttServer_ClientConnected(object sender, MqttClientConnectedEventArgs e)
        {
            Console.WriteLine($"客户端[{e.Client.ClientId}]已连接，协议版本：{e.Client.ProtocolVersion}");
        }

        private static void MqttServer_ClientDisconnected(object sender, MqttClientDisconnectedEventArgs e)
        {
            Console.WriteLine($"客户端[{e.Client.ClientId}]已断开连接！");
            //重新开启
            StartMqttServer();
        }

        private static void MqttServer_ApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            try
            {
                //                //解密密码
                //                var privateKey = @"MIICWwIBAAKBgQCbY8sGqVfzoH0E4MGeOm17QZoqHP1hnEUJ+L6jyr+h9/asQQRa
                //tSwD0lJUpNbPi6w/y4W8FqyhKFolcXwVKAAXKGu3DlImCpOWVb2JW9Uz60GkerzH
                //MYi/Nm5uzgc0hbUpXyzBWU8QxZ+OkB5P4Ieq+whRycq2TizqhpYOkqE77wIDAQAB
                //AoGACJ+YI82AEQgmCABFHnfVnZJ9cLrdIO6gMjZ4tfRJgD6XlOWizTnisG+anBHt
                //zeTNcVjlGhQUDnmDzzImFbJ7lrbLIy238VIp3e+0rffDzd9AMcx0pt9Nx2uKHMVp
                //U1SG7JFspImZvgyOoqif5RL2Xw2J67kP2J3cGNX4pzxszIECQQCh07okY/fTmXgz
                //o2NpVQD1pzHXzRTEH5QclfjU0q7lJ6ZaMo94U8GfaqiVH1eE3K/JAeZ7LFtLxYr/
                //Vk/qtESvAkEA9dETBpHUQHdUkq3YNT6Ni2I0XjTLawrMRC5z5TVQfq2sGfAh0DWv
                //ayNtUVKcpymbi31bHoD0wbzYYRCAAWLMwQJAWmB9z78I9GL8j5JLfdMcYxVKL+R4
                //GYQtWr2jJ3C2foJjVHJyT9gvBZIyrn2/ihMaFV97UgUWw72CgFG69jBRPwJAE3aD
                //YCDJwnTwUFDNbqHOSTv0U4UwmgAX3kojSQGopu8PUlpuAvNNOVlrvWWiG6Yyt5+s
                //SEUDnBdctoq8598vwQJAP/JdbsZ4iHaZg7zGrC42FYePQCOlvc+qslksFWHXodHS
                //b/QGpk/4Wp+xG7sWei6J8WopM7hgiT5N0PlL1sa2Mg==";
                //                var pubkey = "MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCbY8sGqVfzoH0E4MGeOm17QZoqHP1hnEUJ+L6jyr+h9/asQQRatSwD0lJUpNbPi6w/y4W8FqyhKFolcXwVKAAXKGu3DlImCpOWVb2JW9Uz60GkerzHMYi/Nm5uzgc0hbUpXyzBWU8QxZ+OkB5P4Ieq+whRycq2TizqhpYOkqE77wIDAQAB";
                //                var rsa = new RSACryptoService(privateKey, pubkey);

                //var str = $">> {Encoding.UTF8.GetString(e.ApplicationMessage.Payload)}{Environment.NewLine}";
                var result = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                //写到Redis缓存
                RedisHelper<string> redis = new RedisHelper<string>();
                var requestTxt = JsonConvert.DeserializeObject<BackModel>(result);
                if (requestTxt != null && !string.IsNullOrEmpty(requestTxt.BackRequestId))
                {
                    if (!requestTxt.FileIndex.HasValue)
                    {
                        redis.Set(requestTxt.BackRequestId, requestTxt.Result);
                    }
                    else
                    {
                        //分割处理
                        //RedisHelper<List<BackModel>> redis2 = new RedisHelper<List<BackModel>>();
                        if (!backFiles.Keys.Contains(requestTxt.BackRequestId))
                        {
                            backFiles[requestTxt.BackRequestId] = null;
                        }
                        var redisList = backFiles[requestTxt.BackRequestId];
                        if (redisList == null)
                        {
                            backFiles[requestTxt.BackRequestId] = new List<BackModel> { requestTxt };
                        }
                        else
                        {
                            if (redisList.FirstOrDefault(o => o.FileIndex == requestTxt.FileIndex) == null)
                            {
                                redisList.Add(requestTxt);
                                backFiles[requestTxt.BackRequestId] = redisList;
                            }
                        }
                        //组合文件
                        if (requestTxt.IsLastIndex > 0)
                        {
                            if (requestTxt.IsLastIndex == redisList.Count - 1)
                            {
                                redisList = backFiles[requestTxt.BackRequestId];
                                var array = redisList.OrderBy(o => o.FileIndex).Select(o => o.Result).ToList();
                                var newStr = string.Join("", array);
                                redis.Set(requestTxt.BackRequestId, newStr);

                                backFiles.Remove(requestTxt.BackRequestId);
                            }
                        }
                    }

                }
            }
            catch (Exception exception)
            {

            }
        }

        private static void MqttNetTrace_TraceMessagePublished(object sender, MqttNetTraceMessagePublishedEventArgs e)
        {
            //Console.WriteLine($">> 线程ID：{e.ThreadId} 来源：{e.Source} 跟踪级别：{e.Level} 消息: {e.Message}");

            if (e.Exception != null)
            {
                Console.WriteLine(e.Exception);
            }
        }


        #endregion
        

        /// <summary>
        /// 按字符串长度切分成数组
        /// </summary>
        /// <param name="str">原字符串</param>
        /// <param name="separatorCharNum">切分长度</param>
        /// <returns>字符串数组</returns>
        public static List<string> SplitByLen(string str, int separatorCharNum)
        {
            if (string.IsNullOrEmpty(str) || str.Length <= separatorCharNum)
            {
                return new List<string> { str };
            }
            string tempStr = str;
            List<string> strList = new List<string>();
            int iMax = Convert.ToInt32(Math.Ceiling(str.Length / (separatorCharNum * 1.0)));//获取循环次数
            for (int i = 1; i <= iMax; i++)
            {
                string currMsg = tempStr.Substring(0, tempStr.Length > separatorCharNum ? separatorCharNum : tempStr.Length);
                strList.Add(currMsg);
                if (tempStr.Length > separatorCharNum)
                {
                    tempStr = tempStr.Substring(separatorCharNum, tempStr.Length - separatorCharNum);
                }
            }
            return strList;
        }
    }
}
