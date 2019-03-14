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
    public sealed class HttpHandler : ChannelHandlerAdapter //SimpleChannelInboundHandler<IFullHttpRequest>
    {
        public override void ChannelActive(IChannelHandlerContext contex)
        {
            IAttribute<string> der = contex.GetAttribute(AttributeMapConstant.HttpAttriKey);
            if (string.IsNullOrWhiteSpace(der.Get()))
            {
                der.SetIfAbsent($"会重置:{GetType().Name}");
            }
            IAttribute<string> parentAtt = contex.Channel.Parent.GetAttribute(AttributeMapConstant.HttpAttriKey);
            if (string.IsNullOrWhiteSpace(parentAtt.Get()))
            {
                parentAtt.SetIfAbsent($"不会重置:{GetType().Name}");
            }
        }
        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine($"{nameof(HttpHandler)} {{0}}", e);
            ctx.CloseAsync();
        }
        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }
        public async override void ChannelRead(IChannelHandlerContext context, object message)
        {
            if (message is IFullHttpRequest httpRequest)
            {
                var data = await HttpClient.InitializeCreate().GetChannelReadAsync(httpRequest);
                await context.WriteAndFlushAsync(data);
                ReferenceCountUtil.Release(httpRequest);
            }
            else
            {
                context.FireChannelRead(message);
            }
        }
        //protected async override void ChannelRead0(IChannelHandlerContext ctx, IFullHttpRequest msg)
        //{
        //    //IFullHttpRequest fullHttpRequest =(IFullHttpRequest)msg.Copy();
        //    var data = await HttpClient.InitializeCreate().GetChannelReadAsync(msg);
        //    await ctx.WriteAndFlushAsync(data);
        //    ReferenceCountUtil.Release(msg);
        //    //ctx.Flush();
        //    //await ctx.CloseAsync();
        //}
    }
}
