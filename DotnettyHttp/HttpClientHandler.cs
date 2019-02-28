using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Transport.Channels;
using System;
using System.Collections.Generic;
using System.Text;

namespace DotnettyHttp
{
    public sealed class HttpClientHandler: SimpleChannelInboundHandler<IFullHttpResponse>
    {
        //public IByteBuffer Data { get;private set; }
        public IByteBufferHolder responData { get;private set; }
        protected override void ChannelRead0(IChannelHandlerContext ctx, IFullHttpResponse msg)
        {
            //Data = msg.Content.Copy();
            responData =msg.Copy();
            //IByteBuffer byteBuf = msg.Content;
            //Data = byteBuf.ToString(Encoding.UTF8);
        }
    }
}
