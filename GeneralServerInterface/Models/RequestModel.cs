using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace GeneralServerInterface.Models
{
    public class RequestModel
    {
        /// <summary>
        /// 回话ID
        /// </summary>
        public string RequestId { get; set; }
        /// <summary>
        /// 请求路径
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 请求参数
        /// </summary>
        public string Paramas { get; set; }
        /// <summary>
        /// 请求方式
        /// </summary>
        public string Methed { get; set; }
        /// <summary>
        /// 请求类型
        /// </summary>
        public string ContentType { get; set; }
        /// <summary>
        /// 文件Base64字符串
        /// </summary>
        public List<Base64FileInfo> FileBase64Str { get; set; }
    }
    public class Base64FileInfo
    {
        public string FileName { get; set; }
        public string FileBase64Str { get; set; }

        /// <summary>
        /// 文件ID
        /// </summary>
        public string FileId { get; set; }
        /// <summary>
        /// 分割文件顺序
        /// </summary>
        public int FileIndex { get; set; }
        /// <summary>
        /// 大于0则是最后一个的长度
        /// </summary>
        public int IsLastIndex { get; set; }
    }
    public class BackModel
    {
        /// <summary>
        /// 回话ID
        /// </summary>
        public string BackRequestId { get; set; }
        /// <summary>
        /// api请求返回结果
        /// </summary>
        public string Result { get; set; }
        /// <summary>
        /// 分割文件顺序 (为null则是未分割)
        /// </summary>
        public int? FileIndex { get; set; }
        /// <summary>
        /// 大于0则是最后一个的长度
        /// </summary>
        public int IsLastIndex { get; set; }
    }
}