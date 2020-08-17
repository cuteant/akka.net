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

        public override RemoteWatcher.HeartbeatRsp Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var uid = reader.ReadUInt64();

            return new RemoteWatcher.HeartbeatRsp((int)uid);
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, RemoteWatcher.HeartbeatRsp value, IFormatterResolver formatterResolver)
        {
            if (value is null) { writer.WriteNil(ref idx); return; }

            writer.WriteUInt64((ulong)value.AddressUid, ref idx);
        }
    }

    #endregion

    #region -- RemoteRouterConfig --

    public sealed class RemoteRouterConfigFormatter : Akka.Serialization.Formatters.MiscMessageFormatter<RemoteRouterConfig>
    {
        public static readonly IMessagePackFormatter<RemoteRouterConfig> Instance = new RemoteRouterConfigFormatter();

        private RemoteRouterConfigFormatter() { }

        public override RemoteRouterConfig Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<ProtocolRemoteRouterConfig>();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            return new RemoteRouterConfig(
                formatterResolver.Deserialize(protoMessage.Local).AsInstanceOf<Pool>(),
                protoMessage.Nodes.Select(_ => AddressFrom(_)));
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, RemoteRouterConfig value, IFormatterResolver formatterResolver)
        {
            if (value is null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new ProtocolRemoteRouterConfig(
                formatterResolver.SerializeMessage(value.Local),
                value.Nodes.Select(AddressMessageBuilder).ToArray()
            );

            var formatter = formatterResolver.GetFormatterWithVerify<ProtocolRemoteRouterConfig>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    #endregion
}
