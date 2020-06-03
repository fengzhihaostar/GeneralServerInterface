using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServiceStack.Redis;

namespace GeneralServerInterface.Redis
{
    public static class RedisManager
    {
        /// <summary>
        /// redis配置文件信息
        /// </summary>
        private static RedisConfigInfo redisConfigInfo = RedisConfigInfo.GetConfig();

        private static PooledRedisClientManager prcm = null;

        /// <summary>
        /// 静态构造方法，初始化链接池管理对象
        /// </summary>
        //static RedisManager()
        //{
        //    CreateManager();
        //}

        /// <summary>
        /// 创建链接池管理对象
        /// </summary>
        private static void CreateManager()
        {
            string[] writeServerList = SplitString(redisConfigInfo.WriteServerList, ",");
            string[] readServerList = SplitString(redisConfigInfo.ReadServerList, ",");

            prcm = new PooledRedisClientManager(readServerList, writeServerList,
                             new RedisClientManagerConfig
                             {
                                 MaxWritePoolSize = redisConfigInfo.MaxWritePoolSize,
                                 MaxReadPoolSize = redisConfigInfo.MaxReadPoolSize,
                                 AutoStart = redisConfigInfo.AutoStart,
                             });
        }

        private static string[] SplitString(string strSource, string split)
        {
            return strSource.Split(split.ToArray());
        }

        /// <summary>
        /// 客户端缓存操作对象IRedisClient
        /// </summary>
        public static dynamic GetClient
        {
            get
            {
                if (prcm == null)
                    CreateManager();
                return prcm.GetClient();
            }
        }
    }
}