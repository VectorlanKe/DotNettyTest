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
        private static readonly object _groupLook=new object();
        public override void ChannelActive(IChannelHandlerContext contex)
        {
            IChannelGroup g = group;
            if (g == null)
            {
                lock (_groupLook)
                {
                    if (group == null)
                    {
                        g = group = new DefaultChannelGroup(contex.Executor);
                    }
                }
            }
            contex.WriteAndFlushAsync(string.Format("Welcome to {0} secure chat server!{1}", Dns.GetHostName(),"&sup;"));
            g.Add(contex.Channel);
        }
        protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
        {
            ctx.WriteAndFlushAsync($"来自服务端的消息：{msg}&sup;");
        }
        public override void ChannelReadComplete(IChannelHandlerContext ctx) => ctx.Flush();

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine("{0}", e.StackTrace);
            ctx.CloseAsync();
        }
        public override bool IsSharable => true;
    }
}
