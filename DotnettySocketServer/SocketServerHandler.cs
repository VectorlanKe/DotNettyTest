using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace DotnettySocketServer
{
    public class SocketServerHandler : SimpleChannelInboundHandler<string>
    {
        static volatile IChannelGroup group;

        public override void ChannelActive(IChannelHandlerContext contex)
        {
            IChannelGroup g = group;
            if (g == null)
            {
                lock (this)
                {
                    if (group == null)
                    {
                        g = group = new DefaultChannelGroup(contex.Executor);
                    }
                }
            }
            contex.WriteAndFlushAsync(string.Format("Welcome to {0} secure chat server!\n", Dns.GetHostName()));
            g.Add(contex.Channel);
        }
        protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
        {
            ctx.WriteAndFlushAsync($"来自服务端的消息：{msg}");
        }
        public override void ChannelReadComplete(IChannelHandlerContext ctx) => ctx.Flush();

        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine("{0}", e.StackTrace);
            ctx.CloseAsync();
        }
    }
}
