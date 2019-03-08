using dotnet_etcd;
using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.Multipart;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Common;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using DotNetty.Transport.Channels.Groups;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HttpVersion = DotNetty.Codecs.Http.HttpVersion;

namespace DotnettyHttp
{
    public sealed class SocketHandler : SimpleChannelInboundHandler<string>
    {
        private static volatile IChannelGroup group;
        public override void ChannelActive(IChannelHandlerContext contex)
        {
            IAttribute<string> der = contex.GetAttribute(AttributeMapConstant.HttpAttriKey);
            IAttribute<IChannelGroup> parentAtt = contex.Channel.Parent.GetAttribute(AttributeMapConstant.SockerGroup);
            IChannelGroup g = parentAtt.Get();
            if (g == null)
            {
                lock (this)
                {
                    if (g == null)
                    {
                        var chennGroup = new DefaultChannelGroup(contex.Executor);
                        g = chennGroup;
                        parentAtt.SetIfAbsent(chennGroup);
                    }
                }
            }
            contex.WriteAndFlushAsync(string.Format("Welcome to {0} secure chat server!{1}", Dns.GetHostName(),"&sup;"));
            g.Add(contex.Channel);
        }
        protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
        {
            ctx.WriteAndFlushAsync($"来自服务端的消息：{msg}&sup;");
            ctx.Channel.Parent.GetAttribute(AttributeMapConstant.SockerGroup).Get().WriteAndFlushAsync("附件一条统一广播&sup;");
        }

        /// <summary>
        /// 用户事件
        /// </summary>
        /// <param name="context"></param>
        /// <param name="evt"></param>
        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            base.UserEventTriggered(context, evt);
        }
    }
}
