using ServiceStack.Redis;
using System.Text;

namespace GeneralServerInterface.Redis
{
    public class RedisQueueOnClient
    {
        private long _db = 0;
        private string _ip = "";
        private int _port = 0;
        private int _lenght = 0;

        public RedisQueueOnClient(string ip, int port = 6379, int length = 1000)
        {
            _ip = ip;
            _port = port;
            _lenght = length;
        }

        //private static RedisClient _client=null;

        private RedisClient GetClient()
        {
            return new RedisClient(_ip, _port, null, this._db);
        }

        /// <summary>
        /// 入队操作
        /// </summary>
        /// <param name="key">队例名称</param>
        /// <param name="value">队例的值</param>
        /// <returns></returns>
        public long LPush(string key, string value)
        {
            using (RedisClient client = GetClient())
            {
                try
                {
                    long lNum = client.LLen(key);
                    if (lNum < _lenght)
                    {
                        byte[] Val = Encoding.UTF8.GetBytes(value);
                        lNum = client.LPush(key, Val);
                    }
                    return lNum;
                }
                catch
                {
                    return -1;
                }
                finally
                {
                    client.Dispose();
                }
            }
        }

        /// <summary>
        /// 出队操作
        /// </summary>
        /// <param name="key">队例名称</param>
        /// <returns></returns>
        public string RPop(string key)
        {
            using (RedisClient client = GetClient())
            {
                try
                {
                    byte[][] Val = client.BRPop(key, 0);
                    if (Val != null)
                    {
                        string value = Encoding.UTF8.GetString(Val[1]);
                        return value;
                    }
                    return null;
                }
                catch
                {
                    return null;
                }
                finally
                {
                    client.Dispose();
                }
            }
        }

        /// <summary>
        /// 获取队列长度
        /// </summary>
        /// <param name="Key"></param>
        /// <returns></returns>
        public long GetQueueLen(string key)
        {
            using (RedisClient client = GetClient())
            {
                try
                {
                    long lNum = client.LLen(key);
                    return lNum;
                }
                catch
                {
                    return -1;
                }
                finally
                {
                    client.Dispose();
                }
            }
        }
    }
}