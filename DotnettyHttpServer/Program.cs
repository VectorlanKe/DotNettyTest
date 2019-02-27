using DotNetty.Codecs.Http;
using DotNetty.Common;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using System;
using System.Net;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace DotnettyHttpServer
{
    class Program
    {
        //static Program()
        //{
        //    ResourceLeakDetector.Level = ResourceLeakDetector.DetectionLevel.Disabled;
        //}
        static void Main(string[] args) => RunServerAsync().Wait();

        static async Task RunServerAsync()
        {
            IEventLoopGroup group, workGroup;
            var dispatcher = new DispatcherEventLoopGroup();
            group = dispatcher;
            workGroup = new WorkerEventLoopGroup(dispatcher);

            try
            {
                var bootstrap = new ServerBootstrap()
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
                IChannel bootstrapChannel = await bootstrap.BindAsync(IPAddress.IPv6Any, 5001);
                Console.WriteLine($"Httpd started. Listening on {bootstrapChannel.LocalAddress}");
                Console.ReadLine();
                await bootstrapChannel.CloseAsync();
            }
            finally
            {
                group.ShutdownGracefullyAsync().Wait();
            }
        }
    }
}
