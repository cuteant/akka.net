using System.Linq;
using Akka.Actor;
using Akka.Remote.Routing;
using Akka.Routing;
using Akka.Serialization;
using Akka.Util.Internal;
using MessagePack;
using MessagePack.Formatters;
using ProtocolRemoteRouterConfig = Akka.Serialization.Protocol.RemoteRouterConfig;

namespace Akka.Remote.Serialization.Formatters
{
    #region -- RemoteWatcher.HeartbeatRsp --

    public sealed class RemoteWatcherHeartbeatRspFormatter : Akka.Serialization.Formatters.MiscMessageFormatter<RemoteWatcher.HeartbeatRsp>
    {
        public static readonly IMessagePackFormatter<RemoteWatcher.HeartbeatRsp> Instance = new RemoteWatcherHeartbeatRspFormatter();

        private RemoteWatcherHeartbeatRspFormatter() { }

        public override RemoteWatcher.HeartbeatRsp Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var uid = MessagePackBinary.ReadUInt64(bytes, offset, out readSize);

            return new RemoteWatcher.HeartbeatRsp((int)uid);
        }

        public override int Serialize(ref byte[] bytes, int offset, RemoteWatcher.HeartbeatRsp value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            return MessagePackBinary.WriteUInt64(ref bytes, offset, (ulong)value.AddressUid);
        }
    }

    #endregion

    #region -- RemoteRouterConfig --

    public sealed class RemoteRouterConfigFormatter : Akka.Serialization.Formatters.MiscMessageFormatter<RemoteRouterConfig>
    {
        public static readonly IMessagePackFormatter<RemoteRouterConfig> Instance = new RemoteRouterConfigFormatter();

        private RemoteRouterConfigFormatter() { }

        public override RemoteRouterConfig Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<ProtocolRemoteRouterConfig>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            return new RemoteRouterConfig(
                formatterResolver.Deserialize(protoMessage.Local).AsInstanceOf<Pool>(),
                protoMessage.Nodes.Select(_ => AddressFrom(_)));
        }

        public override int Serialize(ref byte[] bytes, int offset, RemoteRouterConfig value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new ProtocolRemoteRouterConfig(
                formatterResolver.Serialize(value.Local),
                value.Nodes.Select(AddressMessageBuilder).ToArray()
            );

            var formatter = formatterResolver.GetFormatterWithVerify<ProtocolRemoteRouterConfig>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    #endregion
}
