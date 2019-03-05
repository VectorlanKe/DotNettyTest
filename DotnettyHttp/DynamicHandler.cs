using dotnet_etcd;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Codecs.Http;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnettyHttp
{
    public class DynamicHandler : ByteToMessageDecoder
    {
        private IByteBuffer delimiter;
        private EtcdClient etcdClient;
        private IByteBuffer webSocketBuffer = Unpooled.WrappedBuffer(Encoding.UTF8.GetBytes("Upgrade: websocket"));


        public DynamicHandler(IByteBuffer delimiterBuff)
        {
            delimiter = delimiterBuff;
        }
        public DynamicHandler(string etcdHost,int etcdPort, IByteBuffer delimiterBuff)
            :this(delimiterBuff)
        {
            etcdClient = new EtcdClient(etcdHost, etcdPort);
        }
        protected override void Decode(IChannelHandlerContext context, IByteBuffer input, List<object> output)
        {
            if (IndexOf(input, delimiter) > 0)
            {
                context.Channel.Pipeline.AddLast(new DelimiterBasedFrameDecoder(1048576, delimiter));
                context.Channel.Pipeline.AddLast(new StringEncoder());
                context.Channel.Pipeline.AddLast(new StringDecoder());
                context.Channel.Pipeline.AddLast(new SocketHandler());
            }
            else if (IndexOf(input, webSocketBuffer) > 0)
            {
                context.Channel.Pipeline.AddLast(new WebSocketHandler());
            }
            else{
                context.Channel.Pipeline.AddLast(new HttpServerCodec());
                context.Channel.Pipeline.AddLast(new HttpObjectAggregator(1048576));
                context.Channel.Pipeline.AddLast(new HttpHandler(etcdClient));
            }
            //output.Add(input);
            context.Channel.Pipeline.Remove(this);
        }
        static int IndexOf(IByteBuffer haystack, IByteBuffer needle)
{
    for (int i = haystack.ReaderIndex; i < haystack.WriterIndex; i++)
    {
        int haystackIndex = i;
        int needleIndex;
        for (needleIndex = 0; needleIndex < needle.Capacity; needleIndex++)
        {
            if (haystack.GetByte(haystackIndex) != needle.GetByte(needleIndex))
            {
                break;
            }
            else
            {
                haystackIndex++;
                if (haystackIndex == haystack.WriterIndex && needleIndex != needle.Capacity - 1)
                {
                    return -1;
                }
            }
        }

        if (needleIndex == needle.Capacity)
        {
            // Found the needle from the haystack!
            return i - haystack.ReaderIndex;
        }
    }
    return -1;
}
    }
}
