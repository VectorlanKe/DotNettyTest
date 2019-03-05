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
        /// <summary>
        /// WebSocker握手路径
        /// </summary>
        private const string websocketPath = "/websocket";
        private WebSocketServerHandshaker handshaker;
        static volatile IChannelGroup group;

        Type httpRequest = typeof(IFullHttpRequest);

        public HttpHandler(EtcdClient etcd)
        {
            httpClient = HttpClient.InitializeCreate();
            etcdClient = etcd;
        }

        public override void ChannelActive(IChannelHandlerContext contex)
        {
            //IChannelGroup g = group;
            //if (g == null)
            //{
            //    lock (this)
            //    {
            //        if (group == null)
            //        {
            //            g = group = new DefaultChannelGroup(contex.Executor);
            //        }
            //    }
            //}

            ////contex.WriteAndFlushAsync(string.Format("Welcome to {0} secure chat server!\n", Dns.GetHostName()));
            //g.Add(contex.Channel);
            base.ChannelActive(contex);
        }
        protected override void ChannelRead0(IChannelHandlerContext ctx, IFullHttpRequest msg)
        {
            HandleHttpRequest(ctx, msg);
            //if (requestsl is IFullHttpRequest request)
            //{
                
            //}
            //else if (requestsl is WebSocketFrame frame)
            //{
            //    HandleWebSocketFrame(ctx, frame);
            //}
        }
        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine($"{nameof(HttpHandler)} {0}", e);
            ctx.CloseAsync();
        }

        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();

        #region Http
        private void HandleHttpRequest(IChannelHandlerContext ctx, IFullHttpRequest request)
        {
            //如果是websocker
            if (request.Uri == websocketPath)
            {
                WebSockerHandshake(ctx, request);
                return;
            }
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
        #endregion

        #region WebSocker
        /// <summary>
        /// WebSocker握手
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="request"></param>
        private void WebSockerHandshake(IChannelHandlerContext ctx, IFullHttpRequest request)
        {
            //获取websocker路径
            bool result = request.Headers.TryGet(HttpHeaderNames.Host, out ICharSequence value);
            Debug.Assert(result, "Host header does not exist.");
            string location = value.ToString() + websocketPath;
            // Handshake 握手
            var wsFactory = new WebSocketServerHandshakerFactory(
                location, null, true, 5 * 1024 * 1024);
            this.handshaker = wsFactory.NewHandshaker(request);
            if (this.handshaker == null)
            {
                WebSocketServerHandshakerFactory.SendUnsupportedVersionResponse(ctx.Channel);
            }
            else
            {
                this.handshaker.HandshakeAsync(ctx.Channel, request);
            }
        }
        /// <summary>
        /// WebSocker状态
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="frame"></param>
        private void HandleWebSocketFrame(IChannelHandlerContext ctx, WebSocketFrame frame)
        {
            // Check for closing frame
            if (frame is CloseWebSocketFrame)
            {
                this.handshaker.CloseAsync(ctx.Channel, (CloseWebSocketFrame)frame.Retain());
                return;
            }

            if (frame is PingWebSocketFrame)
            {
                ctx.WriteAsync(new PongWebSocketFrame((IByteBuffer)frame.Content.Retain()));
                return;
            }

            if (frame is TextWebSocketFrame)
            {
                // Echo the frame
                ctx.WriteAsync(frame.Retain());
                //group.WriteAndFlushAsync("hello");
                return;
            }

            if (frame is BinaryWebSocketFrame)
            {
                // Echo the frame
                ctx.WriteAsync(frame.Retain());
            }
        }
        #endregion

    }
}
