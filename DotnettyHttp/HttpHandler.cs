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
    public sealed class HttpHandler : SimpleChannelInboundHandler<IFullHttpRequest>
    {
        private HttpClient httpClient;
        private EtcdClient etcdClient;
        private Random random = new Random();

        public HttpHandler(EtcdClient etcd)
        {
            httpClient = HttpClient.InitializeCreate();
            etcdClient = etcd;
        }

        public override void ChannelActive(IChannelHandlerContext contex)
        {
            IAttribute<string> der = contex.GetAttribute(AttributeMapConstant.HttpAttriKey);
            if (string.IsNullOrWhiteSpace(der.Get()))
            {
                der.SetIfAbsent($"会重置:{GetType().Name}");
            }
            var parentAtt = contex.Channel.Parent.GetAttribute(AttributeMapConstant.HttpAttriKey);
            if (string.IsNullOrWhiteSpace(parentAtt.Get()))
            {
                parentAtt.SetIfAbsent($"不会重置:{GetType().Name}");
            }
        }
        protected override void ChannelRead0(IChannelHandlerContext ctx, IFullHttpRequest msg)
        {
            HandleHttpRequest(ctx, msg);
        }
        private void HandleHttpRequest(IChannelHandlerContext ctx, IFullHttpRequest request)
        {
            IFullHttpRequest fullHttp = (IFullHttpRequest)request.Copy();
            Task.Run(() =>
            {
                try
                {
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    string requerUrl = Regex.Split(fullHttp.Uri, "(/\\d+)|\\?").FirstOrDefault().ToLower();
                    var urls = etcdClient.GetRangeVal($"{requerUrl}#")?.ToList();
                        IFullHttpResponse responData = null;
                    if (urls?.Count > 0)
                    {
                        KeyValuePair<string, string> url = urls[random.Next(0, urls.Count)];
                        Uri uri = new Uri(url.Value);
                        DefaultFullHttpRequest forwardRequest = new DefaultFullHttpRequest(HttpVersion.Http11, fullHttp.Method, uri.ToString(), fullHttp.Content, fullHttp.Headers, fullHttp.Headers);
                        responData = httpClient.GetChannelRead(forwardRequest).Result;
                    }
                    DefaultFullHttpResponse fullResponse = responData != null ?
                        new DefaultFullHttpResponse(HttpVersion.Http11, responData.Status, responData.Content, responData.Headers, responData.Headers) :
                        new DefaultFullHttpResponse(HttpVersion.Http11, HttpResponseStatus.NotFound, Unpooled.Empty, false);
                    ctx.WriteAndFlushAsync(fullResponse);
                    stopwatch.Stop();
                    
                    Console.WriteLine("请求{0} \r\n耗时：{1}ms\r\n\r\n", request.Uri, stopwatch.ElapsedMilliseconds);
                }
                finally
                {
                    ctx.FireChannelRead(request);
                    ctx.FireChannelRead(fullHttp);
                    ctx.CloseAsync();
                }
            });
        }

    }
}
