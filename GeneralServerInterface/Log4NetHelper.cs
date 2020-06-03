using log4net;
using log4net.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GeneralServerInterface
{
    /// <summary>
    /// Log4net帮助类
    /// </summary>
    public static class Log4NetHelper
    {
        /// <summary>
        /// 消息队列
        /// </summary>
        private static Queue<Log4netModel> logQueue = new Queue<Log4netModel>();

        /// <summary>
        /// 标志锁
        /// </summary>
        private static string myLock = "true";

        /// <summary>
        /// 是否开始自动记录日志
        /// </summary>
        private static bool isStart = false;

        static Log4NetHelper()
        {
            log4net.Config.XmlConfigurator.Configure();
        }

        /// <summary>
        /// 获取log4net
        /// </summary>
        public static ILog Get(string name)
        {
            //配置log4Net信息
            //log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(System.Web.HttpContext.Current.Server.MapPath(@"~/Config/Log4net.config")));
            //log4net.Config.XmlConfigurator.Configure();
            return log4net.LogManager.GetLogger(name);
        }

        /// <summary>
        /// 同步写日志
        /// </summary>
        public static void WriteLog(this ILog log, string message, Level lev = null)
        {
            if (log != null && !string.IsNullOrEmpty(message))
            {
                if (lev == null)
                {
                    lev = Level.Info;
                }

                //写日志
                if (lev == Level.Info)
                {
                    log.Info(message);
                }
                if (lev == Level.Warn)
                {
                    log.Warn(message);
                }
                if (lev == Level.Error)
                {
                    log.Error(message);
                }
            }
        }

        /// <summary>
        /// 异步写日期
        /// </summary>
        public static void WriteLogAsync(this ILog log, string message, Level lev = null)
        {
            // 这里需要锁上 不然会出现：源数组长度不足。请检查 srcIndex 和长度以及数组的下限。异常
            //网上有资料说 http://blog.csdn.net/greatbody/article/details/26135057  不能多线程同时写入队列
            //其实  不仅仅 不能同时写入队列 也不能同时读和写如队列  所以  在Dequeue 取的时候也要锁定一个对象
            lock (myLock)
                logQueue.Enqueue(new Log4netModel(message, configName: log.Logger.Name, lev: lev));
            AsyncStart();
        }

        /// <summary>
        /// 异步写日志
        /// </summary>
        private static void AsyncStart()
        {
            if (isStart)
                return;
            isStart = true;
            Task.Run(() =>
            {
                while (true)
                {
                    if (logQueue.Count >= 1)
                    {
                        Log4netModel model = null;
                        lock (myLock)
                            model = logQueue.Dequeue();
                        if (model == null)
                            continue;
                        if (string.IsNullOrEmpty(model.ConfigName))
                            continue;

                        ILog _logger = log4net.LogManager.GetLogger(model.ConfigName);
                        if (_logger == null)
                            continue;

                        if (model.Level == null)
                        {
                            model.Level = Level.Info;
                        }

                        //写日志
                        if (model.Level == Level.Info)
                        {
                            _logger.Info(model.Message);
                        }
                        if (model.Level == Level.Warn)
                        {
                            _logger.Warn(model.Message);
                        }
                        if (model.Level == Level.Error)
                        {
                            _logger.Error(model.Message);
                        }
                    }
                    else
                    {
                        isStart = false;//标记下次可执行
                        break;//跳出循环
                    }
                }
            });
        }
    }

    /// <summary>
    /// 写日志实体
    /// </summary>
    public class Log4netModel
    {
        public Log4netModel(string message, string configName = null, Level lev = null)
        {
            if (lev == null)
            {
                lev = Level.Info;
            }
            Level = lev;
            Message = message;
            ConfigName = configName;
        }

        /// <summary>
        /// 日志级别
        /// </summary>
        public Level Level { get; set; }

        /// <summary>
        /// 日志消息
        /// </summary>
        public string Message
        {
            get;
            set;
        }

        /// <summary>
        /// 使用配置名称
        /// </summary>
        public string ConfigName { get; set; }
    }
}