using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Codecs.Http;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotnettyWebSocketServer
{
    class Program
    {
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
                               .Handler(new LoggingHandler(LogLevel.INFO))
                               .ChildHandler(new ActionChannelInitializer<IChannel>(channel =>
                               {
                                   IChannelPipeline pipeline = channel.Pipeline;
                                   pipeline.AddLast(new HttpServerCodec());
                                   pipeline.AddLast(new HttpObjectAggregator(65536));
                                   pipeline.AddLast(new WebSockerServerHandler());
                               }));
                IChannel bootstrapChannel = await bootstrap.BindAsync(IPAddress.IPv6Any, 5004);
                Console.WriteLine($"Tcp started. Listening on {bootstrapChannel.LocalAddress}");
                Console.ReadLine();
                await bootstrapChannel.CloseAsync();
            }
            finally
            {
                workGroup.ShutdownGracefullyAsync().Wait();
                group.ShutdownGracefullyAsync().Wait();
            }
        }
    }
}
