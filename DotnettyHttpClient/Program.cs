using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using System;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace DotnettyHttpClient
{
    class Program
    {
        static void Main(string[] args)=> RunClientAsync().Wait();
        private static async Task RunClientAsync()
        {
            HttpClientHandler httpClientHandler = new HttpClientHandler();
            IEventLoopGroup group = new DispatcherEventLoopGroup();
            try
            {
                Bootstrap bootstrap = new Bootstrap()
                    .Group(group)
                    .Channel<TcpChannel>()
                    .Option(ChannelOption.SoBacklog, 8192)
                    .Handler(new ActionChannelInitializer<IChannel>(channel =>
                    {
                        IChannelPipeline pipeline = channel.Pipeline;
                        pipeline.AddLast("decoder", new HttpResponseDecoder(4096, 8192, 8192, false));
                        pipeline.AddLast("aggregator", new HttpObjectAggregator(1024));
                        pipeline.AddLast("encoder", new HttpRequestEncoder());
                        pipeline.AddLast("deflater", new HttpContentDecompressor());//解压
                        pipeline.AddLast("handler", httpClientHandler);
                    }));

                Stopwatch stopwatch = new Stopwatch();
                while (true)
                {
                    Console.WriteLine("请输入请求地址（http://127.0.0.1:5000/api/values）");
                    string url = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        url = "http://127.0.0.1:5000/api/values";
                    }

                    try
                    {
                        httpClientHandler = new HttpClientHandler();
                        Uri uri = new Uri(url);
                        stopwatch.Reset();
                        stopwatch.Start();
                        IChannel chanel = bootstrap.ConnectAsync(IPAddress.Parse(uri.Host), uri.Port).Result;

                        DefaultFullHttpRequest request = new DefaultFullHttpRequest(DotNetty.Codecs.Http.HttpVersion.Http11, HttpMethod.Get, uri.ToString());
                        HttpHeaders headers = request.Headers;
                        headers.Set(HttpHeaderNames.Host, uri.Authority);

                        chanel.WriteAndFlushAsync(request).Wait();
                        while (true)
                        {
                            if (httpClientHandler.Data != null)
                            {
                                Console.WriteLine("结果:{0}", httpClientHandler.Data);
                                break;
                            }
                        }
                        stopwatch.Stop();
                        Console.WriteLine("耗时:{0}ms", stopwatch.ElapsedMilliseconds);
                        //await chanel.CloseAsync();

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }

                }
            }
            finally
            {
                group.ShutdownGracefullyAsync().Wait();
            }
        }
    }



}
