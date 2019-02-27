using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.Multipart;
using DotNetty.Common;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using System;
using System.Text;

namespace DotnettyHttpServer
{
    public sealed class HttpServerHandler : SimpleChannelInboundHandler<IFullHttpRequest>//ChannelHandlerAdapter
    {
        protected override void ChannelRead0(IChannelHandlerContext ctx, IFullHttpRequest msg)
        {
            StringBuilder stringBuilder = new StringBuilder("请求参数：\r\n");
            {
                if (msg.Method == HttpMethod.Get)
                {
                    var fer = new QueryStringDecoder(msg.Uri);
                    foreach (var item in fer.Parameters)
                    {
                        stringBuilder.AppendFormat("{0}:{1}\r\n", item.Key, item.Value[0]);
                    }
                }
                if (msg.Method == HttpMethod.Post)
                {
                    var postRequestDecoder = new HttpPostRequestDecoder(msg).Offer(msg);
                    foreach (var item in postRequestDecoder.GetBodyHttpDatas())
                    {
                        var mixedAttribute = postRequestDecoder.Next() as MixedAttribute;
                        stringBuilder.AppendFormat("{0}:{1}\r\n", mixedAttribute?.Name, mixedAttribute?.Value);
                        mixedAttribute.Release();
                    }
                }
                byte[] text = Encoding.UTF8.GetBytes(stringBuilder.ToString());
                WriteResponse(ctx, Unpooled.WrappedBuffer(text), AsciiString.Cached("text/plain"), AsciiString.Cached(text.Length.ToString()));

            }
        }
        private void WriteResponse(IChannelHandlerContext ctx, IByteBuffer buf, ICharSequence contentType, ICharSequence contentLength)
        {
            // Build the response object.
            var response = new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.OK, buf, false);
            HttpHeaders headers = response.Headers;
            headers.Set(HttpHeaderNames.ContentType, contentType);
            //headers.Set(HttpHeaderNames.Server, "Netty");
            //headers.Set(HttpHeaderNames.Date, this.date);
            headers.Set(HttpHeaderNames.ContentLength, contentLength);
            // Close the non-keep-alive connection after the write operation is done.
            ctx.WriteAndFlushAsync(response);
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) => context.CloseAsync();

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();
    }
}
