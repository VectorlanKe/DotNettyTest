using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels.Groups;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnettyHttp
{
    public class AttributeMapConstant
    {
        /// <summary>
        /// 测试string
        /// </summary>
        public static readonly AttributeKey<string> HttpAttriKey = AttributeKey<string>.ValueOf("httpAttriKey");
        /// <summary>
        /// socket连接数据
        /// </summary>
        public static readonly AttributeKey<IChannelGroup> SockerGroup = AttributeKey<IChannelGroup>.ValueOf("sockerGroup");
    }
}
