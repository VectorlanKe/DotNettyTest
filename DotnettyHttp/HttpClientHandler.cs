using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnettyHttp
{
    public sealed class HttpClientHandler: SimpleChannelInboundHandler<IFullHttpResponse>
    {
        //public IByteBuffer Data { get;private set; }
        public IByteBufferHolder responData { get;private set; }
        public DefaultFullHttpResponse defaultFullHttpResponse { get;private set; }
        protected override void ChannelRead0(IChannelHandlerContext ctx, IFullHttpResponse msg)
        {
            //action(new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.NotFound, Unpooled.Empty, false));
            //Data = msg.Content.Copy();
            responData = msg.Copy();
                             //IByteBuffer byteBuf = msg.Content;
                             //Data = byteBuf.ToString(Encoding.UTF8);
            //defaultFullHttpResponse = new DefaultFullHttpResponse(HttpVersion.Http11,msg.Status,msg.Content,msg.Headers, msg.Headers);
            //foreach (var item in msg.Headers)
            //{
            //    defaultFullHttpResponse.Headers.Set(item.Key,item.Value);
            //}
        }
        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine($"{nameof(HttpClientHandler)} {{0}}", e);
            ctx.CloseAsync();
        }
        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            context.Flush();
        }
    }
}
