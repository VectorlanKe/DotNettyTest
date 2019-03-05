using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnettySocketClient
{
    public class SocketClientHandler : SimpleChannelInboundHandler<string>
    {
        protected override void ChannelRead0(IChannelHandlerContext ctx, string msg)
        {
            Console.WriteLine(msg);
        }
        public override void ExceptionCaught(IChannelHandlerContext contex, Exception e)
        {
            Console.WriteLine(DateTime.Now.Millisecond);
            Console.WriteLine(e.StackTrace);
            contex.CloseAsync();
        }
    }
}
