using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Remote.Routing;
using Akka.Routing;
using Akka.Serialization;
using Akka.Util.Internal;
using CuteAnt.Reflection;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Remote.Serialization.Formatters
{
    #region -- class MiscMessageFormatter<T> --

    public abstract class MiscMessageFormatter<T> : IMessagePackFormatter<T>
    {
        protected static readonly IFormatterResolver DefaultResolver = MessagePackSerializer.DefaultResolver;

        public abstract T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);
        public abstract int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver);

        //
        // Generic Routing Pool
        //
        protected static Protocol.GenericRoutingPool GenericRoutingPoolBuilder(ExtendedActorSystem system, Pool pool)
        {
            return new Protocol.GenericRoutingPool(
                (uint)pool.NrOfInstances,
                pool.RouterDispatcher,
                pool.UsePoolDispatcher,
                WrappedPayloadSupport.PayloadToProto(system, pool.Resizer)
            );
        }

        //
        // Address
        //
        protected static Protocol.AddressData AddressMessageBuilder(Address address)
        {
            var message = new Protocol.AddressData(
                address.System,
                address.Host,
                (uint)(address.Port ?? 0),
                address.Protocol);
            return message;
        }

        protected static Address AddressFrom(in Protocol.AddressData addressProto)
        {
            return new Address(
                addressProto.Protocol,
                addressProto.System,
                addressProto.Hostname,
                addressProto.Port == 0 ? null : (int?)addressProto.Port);
        }

        protected static IActorRef ResolveActorRef(ExtendedActorSystem system, string path)
        {
            if (string.IsNullOrEmpty(path)) { return null; }

            return system.Provider.ResolveActorRef(path);
        }
    }

    #endregion

    #region -- Identify --

    public sealed class IdentifyFormatter : MiscMessageFormatter<Identify>
    {
        public static readonly IMessagePackFormatter<Identify> Instance = new IdentifyFormatter();

        private IdentifyFormatter() { }

        public override Identify Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.Identify>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            return new Identify(WrappedPayloadSupport.PayloadFrom(formatterResolver.GetActorSystem(), protoMessage.MessageId));
        }

        public override int Serialize(ref byte[] bytes, int offset, Identify value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoIdentify = new Protocol.Identify(WrappedPayloadSupport.PayloadToProto(formatterResolver.GetActorSystem(), value.MessageId));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.Identify>();
            return formatter.Serialize(ref bytes, offset, protoIdentify, DefaultResolver);
        }
    }

    #endregion

    #region -- ActorIdentity --

    public sealed class ActorIdentityFormatter : MiscMessageFormatter<ActorIdentity>
    {
        public static readonly IMessagePackFormatter<ActorIdentity> Instance = new ActorIdentityFormatter();

        private ActorIdentityFormatter() { }

        public override ActorIdentity Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ActorIdentity>();
            var system = formatterResolver.GetActorSystem();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            return new ActorIdentity(WrappedPayloadSupport.PayloadFrom(system, protoMessage.CorrelationId), ResolveActorRef(system, protoMessage.Path));
        }

        public override int Serialize(ref byte[] bytes, int offset, ActorIdentity value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoIdentify = new Protocol.ActorIdentity(
                WrappedPayloadSupport.PayloadToProto(formatterResolver.GetActorSystem(), value.MessageId),
                Akka.Serialization.Serialization.SerializedActorPath(value.Subject)
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ActorIdentity>();
            return formatter.Serialize(ref bytes, offset, protoIdentify, DefaultResolver);
        }
    }

    #endregion

    #region -- RemoteWatcher.HeartbeatRsp --

    public sealed class RemoteWatcherHeartbeatRspFormatter : MiscMessageFormatter<RemoteWatcher.HeartbeatRsp>
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

    #region -- Address --

    public sealed class AddressFormatter : MiscMessageFormatter<Address>
    {
        public static readonly IMessagePackFormatter<Address> Instance = new AddressFormatter();

        private AddressFormatter() { }

        public override Address Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var startOffset = offset;

            var system = MessagePackBinary.ReadString(bytes, offset, out readSize);
            offset += readSize;
            var hostname = MessagePackBinary.ReadString(bytes, offset, out readSize);
            offset += readSize;
            var port = MessagePackBinary.ReadUInt32(bytes, offset, out readSize);
            offset += readSize;
            var protocol = MessagePackBinary.ReadString(bytes, offset, out readSize);

            readSize += offset - startOffset;

            return new Address(
                protocol,
                system,
                hostname,
                port == 0 ? null : (int?)port);
        }

        public override int Serialize(ref byte[] bytes, int offset, Address value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var startOffset = offset;

            offset += MessagePackBinary.WriteString(ref bytes, offset, value.System);
            offset += MessagePackBinary.WriteString(ref bytes, offset, value.Host);
            offset += MessagePackBinary.WriteUInt32(ref bytes, offset, (uint)(value.Port ?? 0));
            offset += MessagePackBinary.WriteString(ref bytes, offset, value.Protocol);

            return offset - startOffset;
        }
    }

    #endregion

    #region -- RemoteScope --

    public sealed class RemoteScopeFormatter : MiscMessageFormatter<RemoteScope>
    {
        public static readonly IMessagePackFormatter<RemoteScope> Instance = new RemoteScopeFormatter();

        public RemoteScopeFormatter() { }

        public override RemoteScope Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.RemoteScope>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            return new RemoteScope(AddressFrom(protoMessage.Node));
        }

        public override int Serialize(ref byte[] bytes, int offset, RemoteScope value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.RemoteScope(AddressMessageBuilder(value.Address));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.RemoteScope>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- Config --

    public sealed class ConfigFormatter : MiscMessageFormatter<Config>
    {
        public static readonly IMessagePackFormatter<Config> Instance = new ConfigFormatter();

        private ConfigFormatter() { }

        public override Config Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return Config.Empty; }

            var raw = MessagePackBinary.ReadString(bytes, offset, out readSize);

            return ConfigurationFactory.ParseString(raw);
        }

        public override int Serialize(ref byte[] bytes, int offset, Config value, IFormatterResolver formatterResolver)
        {
            if (value.IsNullOrEmpty()) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            return MessagePackBinary.WriteString(ref bytes, offset, value.Root.ToString());
        }
    }

    #endregion

    #region -- FromConfig --

    public sealed class FromConfigFormatter : MiscMessageFormatter<FromConfig>
    {
        public static readonly IMessagePackFormatter<FromConfig> Instance = new FromConfigFormatter();

        private FromConfigFormatter() { }

        public override FromConfig Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return FromConfig.Instance; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.FromConfig>();
            var fromConfig = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var rawResizer = fromConfig.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(formatterResolver.GetActorSystem(), rawResizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(fromConfig.RouterDispatcher)
                ? fromConfig.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new FromConfig(resizer, Pool.DefaultSupervisorStrategy, routerDispatcher);
        }

        public override int Serialize(ref byte[] bytes, int offset, FromConfig fromConfig, IFormatterResolver formatterResolver)
        {
            if (fromConfig == FromConfig.Instance || null == fromConfig) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var system = formatterResolver.GetActorSystem();
            var protoMessage = new Protocol.FromConfig(
                WrappedPayloadSupport.PayloadToProto(system, fromConfig.Resizer),
                fromConfig.RouterDispatcher);

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.FromConfig>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- DefaultResizer --

    public sealed class DefaultResizerFormatter : MiscMessageFormatter<DefaultResizer>
    {
        public static readonly IMessagePackFormatter<DefaultResizer> Instance = new DefaultResizerFormatter();

        private DefaultResizerFormatter() { }

        public override DefaultResizer Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.DefaultResizer>();
            var resizer = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            return new DefaultResizer(
                (int)resizer.LowerBound,
                (int)resizer.UpperBound,
                (int)resizer.PressureThreshold,
                resizer.RampupRate,
                resizer.BackoffThreshold,
                resizer.BackoffRate,
                (int)resizer.MessagesPerResize);
        }

        public override int Serialize(ref byte[] bytes, int offset, DefaultResizer defaultResizer, IFormatterResolver formatterResolver)
        {
            if (defaultResizer == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.DefaultResizer(
                (uint)defaultResizer.LowerBound,
                (uint)defaultResizer.UpperBound,
                (uint)defaultResizer.PressureThreshold,
                defaultResizer.RampupRate,
                defaultResizer.BackoffThreshold,
                defaultResizer.BackoffRate,
                (uint)defaultResizer.MessagesPerResize
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.DefaultResizer>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- RoundRobinPool --

    public sealed class RoundRobinPoolFormatter : MiscMessageFormatter<RoundRobinPool>
    {
        public static readonly IMessagePackFormatter<RoundRobinPool> Instance = new RoundRobinPoolFormatter();

        private RoundRobinPoolFormatter() { }

        public override RoundRobinPool Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            var broadcastPool = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var rawResizer = broadcastPool.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(formatterResolver.GetActorSystem(), rawResizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(broadcastPool.RouterDispatcher)
                ? broadcastPool.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new RoundRobinPool(
                (int)broadcastPool.NrOfInstances,
                resizer,
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                broadcastPool.UsePoolDispatcher);
        }

        public override int Serialize(ref byte[] bytes, int offset, RoundRobinPool value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = GenericRoutingPoolBuilder(formatterResolver.GetActorSystem(), value);

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- BroadcastPool --

    public sealed class BroadcastPoolFormatter : MiscMessageFormatter<BroadcastPool>
    {
        public static readonly IMessagePackFormatter<BroadcastPool> Instance = new BroadcastPoolFormatter();

        private BroadcastPoolFormatter() { }

        public override BroadcastPool Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            var broadcastPool = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var rawResizer = broadcastPool.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(formatterResolver.GetActorSystem(), rawResizer)
                : null;
            var routerDispatcher = !string.IsNullOrEmpty(broadcastPool.RouterDispatcher)
                ? broadcastPool.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new BroadcastPool(
                (int)broadcastPool.NrOfInstances,
                resizer,
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                broadcastPool.UsePoolDispatcher);
        }

        public override int Serialize(ref byte[] bytes, int offset, BroadcastPool value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = GenericRoutingPoolBuilder(formatterResolver.GetActorSystem(), value);

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- RandomPool --

    public sealed class RandomPoolFormatter : MiscMessageFormatter<RandomPool>
    {
        public static readonly IMessagePackFormatter<RandomPool> Instance = new RandomPoolFormatter();

        private RandomPoolFormatter() { }

        public override RandomPool Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            var randomPool = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var rawResizer = randomPool.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(formatterResolver.GetActorSystem(), rawResizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(randomPool.RouterDispatcher)
                ? randomPool.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new RandomPool(
                (int)randomPool.NrOfInstances,
                resizer,
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                randomPool.UsePoolDispatcher);
        }

        public override int Serialize(ref byte[] bytes, int offset, RandomPool value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = GenericRoutingPoolBuilder(formatterResolver.GetActorSystem(), value);

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- ScatterGatherFirstCompletedPool --

    public sealed class ScatterGatherFirstCompletedPoolFormatter : MiscMessageFormatter<ScatterGatherFirstCompletedPool>
    {
        public static readonly IMessagePackFormatter<ScatterGatherFirstCompletedPool> Instance = new ScatterGatherFirstCompletedPoolFormatter();

        private ScatterGatherFirstCompletedPoolFormatter() { }

        public override ScatterGatherFirstCompletedPool Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ScatterGatherPool>();
            var scatterGatherFirstCompletedPool = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var rawResizer = scatterGatherFirstCompletedPool.Generic.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(formatterResolver.GetActorSystem(), rawResizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(scatterGatherFirstCompletedPool.Generic.RouterDispatcher)
                ? scatterGatherFirstCompletedPool.Generic.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new ScatterGatherFirstCompletedPool(
                (int)scatterGatherFirstCompletedPool.Generic.NrOfInstances,
                resizer,
                scatterGatherFirstCompletedPool.Within.ToTimeSpan(),
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                scatterGatherFirstCompletedPool.Generic.UsePoolDispatcher);
        }

        public override int Serialize(ref byte[] bytes, int offset, ScatterGatherFirstCompletedPool value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.ScatterGatherPool(
                GenericRoutingPoolBuilder(formatterResolver.GetActorSystem(), value),
                Protocol.Duration.FromTimeSpan(value.Within)
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ScatterGatherPool>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- TailChoppingPool --

    public sealed class TailChoppingPoolFormatter : MiscMessageFormatter<TailChoppingPool>
    {
        public static readonly IMessagePackFormatter<TailChoppingPool> Instance = new TailChoppingPoolFormatter();

        private TailChoppingPoolFormatter() { }

        public override TailChoppingPool Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.TailChoppingPool>();
            var tailChoppingPool = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var rawResizer = tailChoppingPool.Generic.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(formatterResolver.GetActorSystem(), rawResizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(tailChoppingPool.Generic.RouterDispatcher)
                ? tailChoppingPool.Generic.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new TailChoppingPool(
                (int)tailChoppingPool.Generic.NrOfInstances,
                resizer,
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                tailChoppingPool.Within.ToTimeSpan(),
                tailChoppingPool.Interval.ToTimeSpan(),
                tailChoppingPool.Generic.UsePoolDispatcher);
        }

        public override int Serialize(ref byte[] bytes, int offset, TailChoppingPool value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.TailChoppingPool(
                GenericRoutingPoolBuilder(formatterResolver.GetActorSystem(), value),
                Protocol.Duration.FromTimeSpan(value.Within),
                Protocol.Duration.FromTimeSpan(value.Interval)
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.TailChoppingPool>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- ConsistentHashingPool --

    public sealed class ConsistentHashingPoolFormatter : MiscMessageFormatter<ConsistentHashingPool>
    {
        public static readonly IMessagePackFormatter<ConsistentHashingPool> Instance = new ConsistentHashingPoolFormatter();

        private ConsistentHashingPoolFormatter() { }

        public override ConsistentHashingPool Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            var consistentHashingPool = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var rawResizer = consistentHashingPool.Resizer;
            Resizer resizer = rawResizer.NoeEmtpy
                ? (Resizer)WrappedPayloadSupport.PayloadFrom(formatterResolver.GetActorSystem(), rawResizer)
                : null;
            var routerDispatcher = !string.IsNullOrEmpty(consistentHashingPool.RouterDispatcher)
                ? consistentHashingPool.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new ConsistentHashingPool(
                (int)consistentHashingPool.NrOfInstances,
                resizer,
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                consistentHashingPool.UsePoolDispatcher);
        }

        public override int Serialize(ref byte[] bytes, int offset, ConsistentHashingPool value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = GenericRoutingPoolBuilder(formatterResolver.GetActorSystem(), value);

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- RemoteRouterConfig --

    public sealed class RemoteRouterConfigFormatter : MiscMessageFormatter<RemoteRouterConfig>
    {
        public static readonly IMessagePackFormatter<RemoteRouterConfig> Instance = new RemoteRouterConfigFormatter();

        private RemoteRouterConfigFormatter() { }

        public override RemoteRouterConfig Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.RemoteRouterConfig>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            return new RemoteRouterConfig(
                WrappedPayloadSupport.PayloadFrom(formatterResolver.GetActorSystem(), protoMessage.Local).AsInstanceOf<Pool>(),
                protoMessage.Nodes.Select(_ => AddressFrom(_)));
        }

        public override int Serialize(ref byte[] bytes, int offset, RemoteRouterConfig value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.RemoteRouterConfig(
                WrappedPayloadSupport.PayloadToProto(formatterResolver.GetActorSystem(), value.Local),
                value.Nodes.Select(AddressMessageBuilder).ToArray()
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.RemoteRouterConfig>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    #endregion
}
