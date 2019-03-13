using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;
using DotNetty.Transport.Libuv;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotnettyHttp
{
    public class TcpClient:IDisposable
    {
        private HttpClientHandler httpClientHandler;
        private IEventLoopGroup group;
        private Bootstrap bootstrap;
        private TimeSpan timeSpan = new TimeSpan(0, 0, 20);
        string delimiter = "&sup;";

        private static TcpClient tcpClient;
        private static readonly object _readLook = new object();

        private TcpClient()
        {
            httpClientHandler = new HttpClientHandler();
            group = new MultithreadEventLoopGroup();
            bootstrap = new Bootstrap()
                .Group(group)
                .Channel<TcpSocketChannel>()
                .Option(ChannelOption.SoBacklog, 8192)
                .Handler(new ActionChannelInitializer<IChannel>(channel =>
                {
                    IChannelPipeline pipeline = channel.Pipeline;
                    pipeline.AddLast(new DelimiterBasedFrameDecoder(8192, Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes(delimiter))));
                    pipeline.AddLast(new StringEncoder(), new StringDecoder(), new TcpClientHandler());
                }));
        }

        public static TcpClient InitializeCreate()
        {
            if (tcpClient == null)
            {
                lock (_readLook)
                {
                    if (tcpClient == null)
                    {
                        tcpClient = new TcpClient();
                    }
                }
            }
            return tcpClient;
        }

        public async Task<IChannel> CreateChannel(IPEndPoint iPEndPoint)
        {
            return await tcpClient.bootstrap.ConnectAsync(iPEndPoint);
        }
        public void Dispose()
        {
            group.ShutdownGracefullyAsync();
        }

    }
}
