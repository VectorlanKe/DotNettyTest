using DotNetty.Codecs.Http;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotnettyHttp
{
    public class HttpServer:IDisposable
    {
        private IEventLoopGroup group, workGroup;
        private ServerBootstrap serverBootstrap;
        public IChannel bootstrapChannel { get; private set; }
        private static HttpServer httpServer;
        private static readonly object _readoLook = new object();
        private HttpServer()
        {
            var dispatcher = new DispatcherEventLoopGroup();
            group = dispatcher;
            workGroup = new WorkerEventLoopGroup(dispatcher);
            serverBootstrap = new ServerBootstrap()
                            .Group(group, workGroup)
                            .Channel<TcpServerChannel>()
                            .Option(ChannelOption.SoBacklog, 8192)
                            .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                            {
                                IChannelPipeline pipeline = channel.Pipeline;
                                pipeline.AddLast("decoder", new HttpRequestDecoder(4096, 8192, 8192, false));
                                pipeline.AddLast("aggregator", new HttpObjectAggregator(1048576));
                                pipeline.AddLast("deflater", new HttpContentCompressor());//压缩
                                pipeline.AddLast("encoder", new HttpResponseEncoder());
                                pipeline.AddLast("handler", new HttpServerHandler());
                            }));
        }
        /// <summary>
        /// 初始化当前对象
        /// </summary>
        /// <returns></returns>
        public static HttpServer InitializeCreate()
        {
            if (httpServer==null)
            {
                lock (_readoLook)
                {
                    if (httpServer == null)
                    {
                        httpServer =new HttpServer();
                    }
                }
            }
            return httpServer;
        }
        public HttpServer RunServerAsync(int inetPort)
        {
            httpServer.bootstrapChannel = httpServer.serverBootstrap.BindAsync(IPAddress.IPv6Any, inetPort).Result;
            return httpServer;
        }
        public void Dispose()
        {
            group.ShutdownGracefullyAsync().Wait();
        }
    }
}
