using dotnet_etcd;
using DotNetty.Buffers;
using DotNetty.Codecs.Http;
using DotNetty.Codecs.Http.Multipart;
using DotNetty.Codecs.Http.WebSockets;
using DotNetty.Common;
using DotNetty.Common.Utilities;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Embedded;
using DotNetty.Transport.Channels.Groups;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HttpVersion = DotNetty.Codecs.Http.HttpVersion;

namespace DotnettyHttp
{
    public sealed class WebSocketHandler : SimpleChannelInboundHandler<IByteBufferHolder>
    {
        /// <summary>
        /// WebSocker握手路径
        /// </summary>
        private const string websocketPath = "/websocket";
        private WebSocketServerHandshaker handshaker;
        public override void ChannelActive(IChannelHandlerContext contex)
        {
            IAttribute<string> der = contex.GetAttribute(AttributeMapConstant.HttpAttriKey);
            IAttribute<string> parentAtt = contex.Channel.Parent.GetAttribute(AttributeMapConstant.HttpAttriKey);
        }
        protected override void ChannelRead0(IChannelHandlerContext ctx, IByteBufferHolder msg)
        {
            if (msg is IFullHttpRequest request)
            {
                WebSockerHandshake(ctx, request);
            }
            else if (msg is WebSocketFrame frame)
            {
                HandleWebSocketFrame(ctx, frame);
            }
        }

        #region WebSocker
        /// <summary>
        /// WebSocker握手
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="request"></param>
        private void WebSockerHandshake(IChannelHandlerContext ctx, IFullHttpRequest request)
        {
            //获取websocker路径
            bool result = request.Headers.TryGet(HttpHeaderNames.Host, out ICharSequence value);
            string location = value.ToString() + websocketPath;
            // Handshake 握手
            var wsFactory = new WebSocketServerHandshakerFactory(
                location, null, true, 5 * 1024 * 1024);
            this.handshaker = wsFactory.NewHandshaker(request);
            if (this.handshaker == null)
            {
                WebSocketServerHandshakerFactory.SendUnsupportedVersionResponse(ctx.Channel);
            }
            else
            {
                this.handshaker.HandshakeAsync(ctx.Channel, request);
            }
        }
        /// <summary>
        /// WebSocker状态
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="frame"></param>
        private void HandleWebSocketFrame(IChannelHandlerContext ctx, WebSocketFrame frame)
        {
            // Check for closing frame
            if (frame is CloseWebSocketFrame)
            {
                this.handshaker.CloseAsync(ctx.Channel, (CloseWebSocketFrame)frame.Retain());
                return;
            }

            if (frame is PingWebSocketFrame)
            {
                ctx.WriteAsync(new PongWebSocketFrame((IByteBuffer)frame.Content.Retain()));
                return;
            }

            if (frame is TextWebSocketFrame)
            {
                // Echo the frame
                ctx.WriteAsync(frame.Retain());
                //group.WriteAndFlushAsync("hello");
                return;
            }

            if (frame is BinaryWebSocketFrame)
            {
                // Echo the frame
                ctx.WriteAsync(frame.Retain());
            }
        }
        #endregion

    }
}
