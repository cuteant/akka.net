using Akka.Serialization;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Remote.Serialization.Formatters
{
    internal sealed class DaemonMsgCreateFormatter : IMessagePackFormatter<DaemonMsgCreate>
    {
        private static readonly IFormatterResolver DefaultResolver = MessagePackSerializer.DefaultResolver;
        public static readonly IMessagePackFormatter<DaemonMsgCreate> Instance = new DaemonMsgCreateFormatter();

        public DaemonMsgCreate Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.DaemonMsgCreateData>();
            var proto = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var system = formatterResolver.GetActorSystem();
            return new DaemonMsgCreate(
                DaemonMsgCreateSerializer.PropsFromProto(system, proto.Props),
                DaemonMsgCreateSerializer.DeployFromProto(system, proto.Deploy),
                proto.Path,
                DaemonMsgCreateSerializer.DeserializeActorRef(system, proto.Supervisor));
        }

        public int Serialize(ref byte[] bytes, int offset, DaemonMsgCreate value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var system = formatterResolver.GetActorSystem();
            var protoMessage = new Protocol.DaemonMsgCreateData(
                    DaemonMsgCreateSerializer.PropsToProto(system, value.Props),
                    DaemonMsgCreateSerializer.DeployToProto(system, value.Deploy),
                    value.Path,
                    DaemonMsgCreateSerializer.SerializeActorRef(value.Supervisor));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.DaemonMsgCreateData>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }
}
