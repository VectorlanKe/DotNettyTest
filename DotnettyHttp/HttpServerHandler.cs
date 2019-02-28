using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.Multipart;
using DotNetty.Common;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace DotnettyHttp
{
    public sealed class HttpServerHandler : SimpleChannelInboundHandler<IFullHttpRequest>
    {
        private HttpClient httpClient;
        public HttpServerHandler()
        {
            httpClient = HttpClient.InitializeCreate();
        }
        protected override void ChannelRead0(IChannelHandlerContext ctx, IFullHttpRequest msg)
        {
            
            IFullHttpRequest fullHttpRequest = (IFullHttpRequest)msg.Copy();
            Task.Run(()=> {
                try
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    Uri uri = new Uri($"http://127.0.0.1:5000/api{fullHttpRequest.Uri}");
                    IByteBuffer responData = httpClient.GetChannelRead(
                            new DefaultFullHttpRequest(HttpVersion.Http11, fullHttpRequest.Method, uri.ToString(), fullHttpRequest.Content, fullHttpRequest.Headers, fullHttpRequest.Headers)
                        ).Result;
                    if (responData != null)
                    {
                        WriteResponse(ctx, responData, AsciiString.Cached("text/plain"), AsciiString.Cached(responData.WriterIndex.ToString()));
                    }
                    stopwatch.Stop();
                    Console.WriteLine("请求{0} \r\n耗时：{1}ms\r\n\r\n", fullHttpRequest.Uri, stopwatch.ElapsedMilliseconds);
                }
                finally
                {
                    ctx.FireChannelRead(msg);
                    ctx.FireChannelRead(fullHttpRequest);
                }
            });
            
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
