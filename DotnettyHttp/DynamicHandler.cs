using dotnet_etcd;
using DotNetty.Buffers;
using DotNetty.Codecs;
using DotNetty.Codecs.Http;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Groups;
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
        public DynamicHandler(EtcdClient etcd, IByteBuffer delimiterBuff)
            : this(delimiterBuff)
        {
            etcdClient = etcd;
        }
        public override void ChannelActive(IChannelHandlerContext context)
        {
            //var der = contex.GetAttribute(AttributeMapConstant.HttpAttriKey);
            //if (string.IsNullOrWhiteSpace(der.Get()))
            //{
            //    der.SetIfAbsent($"私有1:{GetType().Name}");
            //}
            //Console.WriteLine(der.Get());
            //base.ChannelActive(contex);
            //IAttribute<IChannelGroup> parentAtt = contex.Channel.Parent.GetAttribute(AttributeMapConstant.SockerGroup);
            //IChannelGroup g = parentAtt.Get();
            //if (g == null)
            //{
            //    lock (this)
            //    {
            //        if (g == null)
            //        {
            //            var chennGroup = new DefaultChannelGroup(contex.Executor);
            //            parentAtt.SetIfAbsent(chennGroup);
            //            g = chennGroup;
            //        }
            //    }
            //}
            //contex.WriteAndFlushAsync(string.Format("Welcome to {0} secure chat server!{1}", System.Net.Dns.GetHostName(), "&sup;"));
            //g.Add(contex.Channel);
            var fr = context.Channel;
            //context.FireChannelActive();
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
                context.Channel.Pipeline.AddLast(new HttpServerCodec());
                context.Channel.Pipeline.AddLast(new HttpObjectAggregator(1048576));
                context.Channel.Pipeline.AddLast(new WebSocketHandler());
            }
            else
            {
                context.Channel.Pipeline.AddLast(new HttpServerCodec());
                context.Channel.Pipeline.AddLast(new HttpObjectAggregator(1048576));
                context.Channel.Pipeline.AddLast(new HttpHandler(etcdClient));
            }
            //output.Add(input);
            context.FireChannelActive();
            context.Channel.Pipeline.Remove(this);
        }
        public override void ExceptionCaught(IChannelHandlerContext ctx, Exception e)
        {
            Console.WriteLine($"{nameof(HttpHandler)} {0}", e);
            ctx.CloseAsync();
        }
        public override void ChannelReadComplete(IChannelHandlerContext context) => context.Flush();
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
