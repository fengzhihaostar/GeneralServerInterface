using log4net;
using log4net.Core;
using System;
using System.Collections.Generic;

namespace GeneralServerInterface.Redis
{
    public sealed class RedisHelper<T> where T : class
    {
        public bool Set(string key, T model)
        {
            bool res = false;
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    //var client = redisClient.GetTypedClient<T>();
                    res = client.Set<T>(key, model);
                }
            }
            catch (Exception ex)
            {
                res = false;
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
            }
            return res;
        }

        public bool Set(string key, string value)
        {
            bool res = false;
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    //var client = redisClient.GetTypedClient<T>();
                    res = client.Set(key, value);
                }
            }
            catch (Exception ex)
            {
                res = false;
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
            }
            return res;
        }

        public bool Set(string key, string value, TimeSpan timespan)
        {
            bool res = false;
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    res = client.Set(key, value, timespan);
                }
            }
            catch (Exception ex)
            {
                res = false;
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
            }
            return res;
        }

        public bool Set(string key, T model, TimeSpan timespan)
        {
            bool res = false;
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    res = client.Set<T>(key, model, timespan);
                }
            }
            catch (Exception ex)
            {
                res = false;
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
            }
            return res;
        }

        public bool Set(string key, T model, DateTime expiresAt)
        {
            bool res = false;
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    res = client.Set<T>(key, model, expiresAt);
                }
            }
            catch (Exception ex)
            {
                res = false;
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
            }
            return res;
        }

        public bool SetList(string key, IList<T> list)
        {
            bool res = false;
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    res = client.Set<IList<T>>(key, list);
                }
            }
            catch (Exception ex)
            {
                res = false;
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
            }
            return res;
        }

        public T Get(string key)
        {
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    T model = client.Get<T>(key);
                    return model;
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
                return null;
            }
        }

        public string GetString(string key)
        {
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    string strResult = client.Get<string>(key);
                    return strResult;
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
                return null;
            }
        }

        public IDictionary<string, T> GetList(string[] key)
        {
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    IDictionary<string, T> list = client.GetAll<T>(key);
                    return list;
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
                return null;
            }
        }
        /// <summary>
        /// 模糊查询redis所有的key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<string> GetVagueKeys(string key)
        {
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    return client.SearchKeys(key + "*");
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
                return null;
            }
        }

        public IDictionary<string, string> GetStringList(string[] key)
        {
            try
            {
                using (var client = RedisManager.GetClient)
                {

                    IDictionary<string, string> list = client.GetAll<string>(key);
                    return list;

                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
                return null;
            }
        }

        public bool Remove(string key)
        {
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    bool res = client.Remove(key);
                    return res;
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
                return false;
            }
        }

        public void RemoveAll(string[] keys)
        {
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    client.RemoveAll(keys);
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
            }
        }

        /// <summary>
        /// 删除某个前缀的缓存 一般为表名 TB_Account_
        /// </summary>
        /// <param name="keypat"></param>
        public void RemoveTable(string keypat)
        {
            try
            {
                using (var client = RedisManager.GetClient)
                {

                    List<string> keys = client.SearchKeys(keypat + "*");
                    client.RemoveAll(keys);
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
            }
        }

        /// <summary>
        /// 清除当前数据库
        /// </summary>
        public void FlushDB()
        {
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    client.FlushDb();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
            }
        }

        /// <summary>
        /// 清除整个Redis
        /// </summary>
        public void FlushAll()
        {
            try
            {
                using (var client = RedisManager.GetClient)
                {
                    client.FlushAll();
                }
            }
            catch (Exception ex)
            {
                LogManager.GetLogger(EnumLogType.ServiceLog.ToString()).WriteLogAsync("Redis服务器异常" + ex.ToString(), Level.Error);
            }
        }
    }
    public enum EnumLogType
    {
        /// <summary>
        /// 异常日志
        /// </summary>
        ExceptionLog,

        /// <summary>
        /// 业务日志
        /// </summary>
        ServiceLog,
    }

}
