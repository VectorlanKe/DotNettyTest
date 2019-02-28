﻿using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Libuv;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DotnettyHttp
{
    public class HttpClient:IDisposable
    {
        private HttpClientHandler httpClientHandler;
        private IEventLoopGroup group;
        private Bootstrap bootstrap;
        private TimeSpan timeSpan = new TimeSpan(0, 0, 20);

        private static HttpClient httpClient;
        private static readonly object _readLook = new object();

        private HttpClient()
        {
            httpClientHandler = new HttpClientHandler();
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
                    pipeline.AddLast("handler", httpClientHandler);
                }));
        }

        public static HttpClient InitializeCreate()
        {
            if (httpClient==null)
            {
                lock (_readLook)
                {
                    if (httpClient==null)
                    {
                        httpClient = new HttpClient();
                    }
                }
            }
            return httpClient;
        }

        public async Task<IFullHttpResponse> GetChannelRead(DefaultFullHttpRequest request)
        {
            return await Task.Run(()=> {
                IFullHttpResponse resopnData = null;
                try
                {
                    Uri uri = new Uri(request.Uri);
                    IChannel chanel = httpClient.bootstrap.ConnectAsync(IPAddress.Parse(uri.Host), uri.Port).Result;
                    //DefaultFullHttpRequest request = new DefaultFullHttpRequest(DotNetty.Codecs.Http.HttpVersion.Http11, HttpMethod.Get, uri.ToString());
                    HttpHeaders headers = request.Headers;
                    headers.Set(HttpHeaderNames.Host, uri.Authority);
                    chanel.WriteAndFlushAsync(request).Wait();
                    //IByteBuffer retubf=null;
                    Stopwatch stopwatch = new Stopwatch();
                    stopwatch.Start();
                    while (stopwatch.ElapsedMilliseconds < timeSpan.TotalMilliseconds)
                    {
                        if (httpClient.httpClientHandler.responData != null)
                        {
                            resopnData = (IFullHttpResponse)httpClient.httpClientHandler.responData.Copy();
                            break;
                        }
                    }
                    stopwatch.Stop();
                }
                finally
                {
                    httpClient.httpClientHandler = new HttpClientHandler();
                }
                return resopnData;
            });
        }
        public void Dispose()
        {
            group.ShutdownGracefullyAsync();
        }

    }
}