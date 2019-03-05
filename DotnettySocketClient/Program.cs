using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Common.Internal.Logging;
using DotNetty.Handlers.Logging;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotnettySocketClient
{
    class Program
    {
        static void Main() => RunClientAsync().Wait();

        static async Task RunClientAsync()
        {
            string hell = Console.ReadLine();

            InternalLoggerFactory.DefaultFactory.AddProvider(new ConsoleLoggerProvider((s, level) => true, false));
            string delimiter = "&sup;";
            var group = new MultithreadEventLoopGroup();
            try
            {
                var bootstrap = new Bootstrap();
                bootstrap
                    .Group(group)
                    .Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    //.Option(ChannelOption.SoBacklog, 8192)
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;

                        pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes(delimiter))));
                        pipeline.AddLast(new StringEncoder(), new StringDecoder(), new SocketClientHandler());
                    }));

                IChannel bootstrapChannel = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5003));

                for (;; )
                {
                    string line = Console.ReadLine();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    try
                    {
                        await bootstrapChannel.WriteAndFlushAsync(line + delimiter);
                    }
                    catch
                    {
                    }
                    if (string.Equals(line, "bye", StringComparison.OrdinalIgnoreCase))
                    {
                        await bootstrapChannel.CloseAsync();
                        break;
                    }
                }

                await bootstrapChannel.CloseAsync();
            }
            finally
            {
                group.ShutdownGracefullyAsync().Wait(1000);
            }
        }


    }
}
