using dotnet_etcd;
using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.Multipart;
using DotNetty.Common;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotnettyHttp
{
    public sealed class HttpServerHandler : SimpleChannelInboundHandler<IFullHttpRequest>
    {
        private HttpClient httpClient;
        private EtcdClient etcdClient;
        private Random random = new Random();
        public HttpServerHandler(EtcdClient etcd)
        {
            httpClient = HttpClient.InitializeCreate();
            etcdClient = etcd;
        }
        protected override void ChannelRead0(IChannelHandlerContext ctx, IFullHttpRequest msg)
        {

            IFullHttpRequest fullRequest = (IFullHttpRequest)msg.Copy();
            Task.Run(() =>
            {
                try
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    var urls = etcdClient.GetRangeVal($"{fullRequest.Uri.Split('?')[0].ToLower()}#")?.ToList();
                    //string url = etcdClient.GetVal(fullRequest.Uri.Split('?')[0].ToLower());
                    IFullHttpResponse responData = null;
                    if (urls?.Count>0)
                    {
                        KeyValuePair<string,string> url = urls[random.Next(0, urls.Count)];
                        Uri uri = new Uri(url.Value);
                        DefaultFullHttpRequest forwardRequest = new DefaultFullHttpRequest(HttpVersion.Http11, fullRequest.Method, uri.ToString(), fullRequest.Content, fullRequest.Headers, fullRequest.Headers);
                        responData = httpClient.GetChannelRead(forwardRequest).Result;
                    }
                    DefaultFullHttpResponse fullResponse = responData != null ?
                        new DefaultFullHttpResponse(HttpVersion.Http11, responData.Status, responData.Content, responData.Headers, responData.Headers) :
                        new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.NotFound, Unpooled.Empty, false);
                    ctx.WriteAndFlushAsync(fullResponse);
                    ctx.CloseAsync();
                    stopwatch.Stop();
                    Console.WriteLine("请求{0} \r\n耗时：{1}ms\r\n\r\n", fullRequest.Uri, stopwatch.ElapsedMilliseconds);
                }
                finally
                {
                    ctx.FireChannelRead(msg);
                    ctx.FireChannelRead(fullRequest);
                }
            });

        }
        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception) => context.CloseAsync();

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();
    }
}
