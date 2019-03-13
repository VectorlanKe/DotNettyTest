using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DotnettyHttp
{
    public class HttpClient : IDisposable
    {
        //private HttpClientHandler httpClientHandler;
        private IEventLoopGroup group;
        private Bootstrap bootstrap;
        private TimeSpan timeSpan = new TimeSpan(0, 0, 12);

        private Random random = new Random();

        private Dictionary<string, IChannel> httpChannel = new Dictionary<string, IChannel>();
        private static HttpClient httpClient;
        private static readonly object _readLook = new object();

        private HttpClient()
        {
            //httpClientHandler = new HttpClientHandler();
            group = new DispatcherEventLoopGroup();
            bootstrap = new Bootstrap()
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
                    pipeline.AddLast("handler", new HttpClientHandler());
                }));
        }

        public static HttpClient InitializeCreate()
        {
            if (httpClient == null)
            {
                lock (_readLook)
                {
                    if (httpClient == null)
                    {
                        httpClient = new HttpClient();
                    }
                }
            }
            return httpClient;
        }
        public async Task<IChannel> CreateChannelAsync(string host, int port)
        {
            string key = $"{host}:{port}";
            IChannel channel = httpClient.httpChannel.GetValueOrDefault(key);
            if (channel==null)
            {
                channel = await httpClient.bootstrap.ConnectAsync(IPAddress.Parse(host), port);
                httpClient.httpChannel.Add(key, channel);
            }
            return channel;
        }
        public async Task<DefaultFullHttpResponse> GetChannelReadAsync(IFullHttpRequest request)
        {
            return await Task.Run(async()=> {
                IFullHttpResponse responData = null;
                try
                {
                    string requerUrl = Regex.Split(request.Uri, "(/\\d+)|\\?").FirstOrDefault().ToLower();
                    var urls = DotnettyServer.EtcdClient.GetRangeVal($"{requerUrl}#")?.ToList();
                    if (urls?.Count > 0)
                    {
                        KeyValuePair<string, string> url = urls[random.Next(0, urls.Count)];
                        Uri uri = new Uri(url.Value);
                        DefaultFullHttpRequest forwardRequest = new DefaultFullHttpRequest(DotNetty.Codecs.Http.HttpVersion.Http11, request.Method, uri.ToString(), request.Content, request.Headers, request.Headers);
                        IChannel chanel = await CreateChannelAsync(uri.Host, uri.Port);//httpClient.bootstrap.ConnectAsync(IPAddress.Parse(uri.Host), uri.Port).Result;
                        HttpHeaders headers = forwardRequest.Headers;
                        headers.Set(HttpHeaderNames.Host, uri.Authority);
                        await chanel.WriteAndFlushAsync(forwardRequest);
                        //IByteBuffer retubf=null;
                        HttpClientHandler httpClientHandler = chanel.Pipeline.Get<HttpClientHandler>();
                        Stopwatch stopwatch = new Stopwatch();
                        stopwatch.Start();
                        while (stopwatch.ElapsedMilliseconds < timeSpan.TotalMilliseconds)
                        {
                            if (httpClientHandler.responData != null)
                            {
                                responData = (IFullHttpResponse)httpClientHandler.responData.Copy();
                                break;
                            }
                        }
                        stopwatch.Stop();
                    }
                }
                catch (Exception ex)
                {

                }
                return responData != null ?
                    new DefaultFullHttpResponse(DotNetty.Codecs.Http.HttpVersion.Http11, responData.Status, responData.Content, responData.Headers, responData.Headers) :
                    new DefaultFullHttpResponse(DotNetty.Codecs.Http.HttpVersion.Http11, HttpResponseStatus.NotFound, Unpooled.Empty, false);
            });
        }
        public void Dispose()
        {
            group.ShutdownGracefullyAsync();
        }

    }
}