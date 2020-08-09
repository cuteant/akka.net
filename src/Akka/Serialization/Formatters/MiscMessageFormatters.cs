using Akka.Actor;
using Akka.Dispatch;
using Akka.Routing;
using Akka.Serialization;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Formatters
{
    #region -- class MiscMessageFormatter<T> --

    public abstract class MiscMessageFormatter<T> : IMessagePackFormatter<T>
    {
        protected static readonly IFormatterResolver DefaultResolver = MessagePackSerializer.DefaultResolver;

        public abstract T Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver);
        public abstract void Serialize(ref MessagePackWriter writer, ref int idx, T value, IFormatterResolver formatterResolver);

        //
        // Generic Routing Pool
        //
        protected static Protocol.GenericRoutingPool GenericRoutingPoolBuilder(ExtendedActorSystem system, Pool pool)
        {
            return new Protocol.GenericRoutingPool(
                (uint)pool.NrOfInstances,
                pool.RouterDispatcher,
                pool.UsePoolDispatcher,
                system.SerializeMessage(pool.Resizer)
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

        public override Identify Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.Identify>();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            return new Identify(formatterResolver.Deserialize(protoMessage.MessageId));
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, Identify value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoIdentify = new Protocol.Identify(formatterResolver.SerializeMessage(value.MessageId));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.Identify>();
            formatter.Serialize(ref writer, ref idx, protoIdentify, DefaultResolver);
        }
    }

    #endregion

    #region -- ActorIdentity --

    public sealed class ActorIdentityFormatter : MiscMessageFormatter<ActorIdentity>
    {
        public static readonly IMessagePackFormatter<ActorIdentity> Instance = new ActorIdentityFormatter();

        private ActorIdentityFormatter() { }

        public override ActorIdentity Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ActorIdentity>();
            var system = formatterResolver.GetActorSystem();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            return new ActorIdentity(system.Deserialize(protoMessage.CorrelationId), ResolveActorRef(system, protoMessage.Path));
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, ActorIdentity value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoIdentify = new Protocol.ActorIdentity(
                formatterResolver.SerializeMessage(value.MessageId),
                Akka.Serialization.Serialization.SerializedActorPath(value.Subject)
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ActorIdentity>();
            formatter.Serialize(ref writer, ref idx, protoIdentify, DefaultResolver);
        }
    }

    #endregion

    #region -- RemoteScope --

    public sealed class RemoteScopeFormatter : MiscMessageFormatter<RemoteScope>
    {
        public static readonly IMessagePackFormatter<RemoteScope> Instance = new RemoteScopeFormatter();

        public RemoteScopeFormatter() { }

        public override RemoteScope Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.RemoteScope>();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            return new RemoteScope(AddressFrom(protoMessage.Node));
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, RemoteScope value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new Protocol.RemoteScope(AddressMessageBuilder(value.Address));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.RemoteScope>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- FromConfig --

    public sealed class FromConfigFormatter : MiscMessageFormatter<FromConfig>
    {
        public static readonly IMessagePackFormatter<FromConfig> Instance = new FromConfigFormatter();

        private FromConfigFormatter() { }

        public override FromConfig Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return FromConfig.Instance; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.FromConfig>();
            var fromConfig = formatter.Deserialize(ref reader, DefaultResolver);

            var rawResizer = fromConfig.Resizer;
            Resizer resizer = rawResizer.NonEmtpy()
                ? (Resizer)formatterResolver.Deserialize(rawResizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(fromConfig.RouterDispatcher)
                ? fromConfig.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new FromConfig(resizer, Pool.DefaultSupervisorStrategy, routerDispatcher);
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, FromConfig fromConfig, IFormatterResolver formatterResolver)
        {
            if (fromConfig == FromConfig.Instance || null == fromConfig) { writer.WriteNil(ref idx); return; }

            var system = formatterResolver.GetActorSystem();
            var protoMessage = new Protocol.FromConfig(
                system.SerializeMessage(fromConfig.Resizer),
                fromConfig.RouterDispatcher);

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.FromConfig>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- DefaultResizer --

    public sealed class DefaultResizerFormatter : MiscMessageFormatter<DefaultResizer>
    {
        public static readonly IMessagePackFormatter<DefaultResizer> Instance = new DefaultResizerFormatter();

        private DefaultResizerFormatter() { }

        public override DefaultResizer Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.DefaultResizer>();
            var resizer = formatter.Deserialize(ref reader, DefaultResolver);

            return new DefaultResizer(
                (int)resizer.LowerBound,
                (int)resizer.UpperBound,
                (int)resizer.PressureThreshold,
                resizer.RampupRate,
                resizer.BackoffThreshold,
                resizer.BackoffRate,
                (int)resizer.MessagesPerResize);
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, DefaultResizer defaultResizer, IFormatterResolver formatterResolver)
        {
            if (defaultResizer == null) { writer.WriteNil(ref idx); return; }

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
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- RoundRobinPool --

    public sealed class RoundRobinPoolFormatter : MiscMessageFormatter<RoundRobinPool>
    {
        public static readonly IMessagePackFormatter<RoundRobinPool> Instance = new RoundRobinPoolFormatter();

        private RoundRobinPoolFormatter() { }

        public override RoundRobinPool Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            var broadcastPool = formatter.Deserialize(ref reader, DefaultResolver);

            var rawResizer = broadcastPool.Resizer;
            Resizer resizer = rawResizer.NonEmtpy()
                ? (Resizer)formatterResolver.Deserialize(rawResizer)
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

        public override void Serialize(ref MessagePackWriter writer, ref int idx, RoundRobinPool value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = GenericRoutingPoolBuilder(formatterResolver.GetActorSystem(), value);

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- BroadcastPool --

    public sealed class BroadcastPoolFormatter : MiscMessageFormatter<BroadcastPool>
    {
        public static readonly IMessagePackFormatter<BroadcastPool> Instance = new BroadcastPoolFormatter();

        private BroadcastPoolFormatter() { }

        public override BroadcastPool Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            var broadcastPool = formatter.Deserialize(ref reader, DefaultResolver);

            var rawResizer = broadcastPool.Resizer;
            Resizer resizer = rawResizer.NonEmtpy()
                ? (Resizer)formatterResolver.Deserialize(rawResizer)
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

        public override void Serialize(ref MessagePackWriter writer, ref int idx, BroadcastPool value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = GenericRoutingPoolBuilder(formatterResolver.GetActorSystem(), value);

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- RandomPool --

    public sealed class RandomPoolFormatter : MiscMessageFormatter<RandomPool>
    {
        public static readonly IMessagePackFormatter<RandomPool> Instance = new RandomPoolFormatter();

        private RandomPoolFormatter() { }

        public override RandomPool Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            var randomPool = formatter.Deserialize(ref reader, DefaultResolver);

            var rawResizer = randomPool.Resizer;
            Resizer resizer = rawResizer.NonEmtpy()
                ? (Resizer)formatterResolver.Deserialize(rawResizer)
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

        public override void Serialize(ref MessagePackWriter writer, ref int idx, RandomPool value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = GenericRoutingPoolBuilder(formatterResolver.GetActorSystem(), value);

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- ScatterGatherFirstCompletedPool --

    public sealed class ScatterGatherFirstCompletedPoolFormatter : MiscMessageFormatter<ScatterGatherFirstCompletedPool>
    {
        public static readonly IMessagePackFormatter<ScatterGatherFirstCompletedPool> Instance = new ScatterGatherFirstCompletedPoolFormatter();

        private ScatterGatherFirstCompletedPoolFormatter() { }

        public override ScatterGatherFirstCompletedPool Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ScatterGatherPool>();
            var scatterGatherFirstCompletedPool = formatter.Deserialize(ref reader, DefaultResolver);

            var rawResizer = scatterGatherFirstCompletedPool.Generic.Resizer;
            Resizer resizer = rawResizer.NonEmtpy()
                ? (Resizer)formatterResolver.Deserialize(rawResizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(scatterGatherFirstCompletedPool.Generic.RouterDispatcher)
                ? scatterGatherFirstCompletedPool.Generic.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new ScatterGatherFirstCompletedPool(
                (int)scatterGatherFirstCompletedPool.Generic.NrOfInstances,
                resizer,
                scatterGatherFirstCompletedPool.Within,
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                scatterGatherFirstCompletedPool.Generic.UsePoolDispatcher);
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, ScatterGatherFirstCompletedPool value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new Protocol.ScatterGatherPool(
                GenericRoutingPoolBuilder(formatterResolver.GetActorSystem(), value),
                value.Within
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ScatterGatherPool>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- TailChoppingPool --

    public sealed class TailChoppingPoolFormatter : MiscMessageFormatter<TailChoppingPool>
    {
        public static readonly IMessagePackFormatter<TailChoppingPool> Instance = new TailChoppingPoolFormatter();

        private TailChoppingPoolFormatter() { }

        public override TailChoppingPool Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.TailChoppingPool>();
            var tailChoppingPool = formatter.Deserialize(ref reader, DefaultResolver);

            var rawResizer = tailChoppingPool.Generic.Resizer;
            Resizer resizer = rawResizer.NonEmtpy()
                ? (Resizer)formatterResolver.Deserialize(rawResizer)
                : null;

            var routerDispatcher = !string.IsNullOrEmpty(tailChoppingPool.Generic.RouterDispatcher)
                ? tailChoppingPool.Generic.RouterDispatcher
                : Dispatchers.DefaultDispatcherId;

            return new TailChoppingPool(
                (int)tailChoppingPool.Generic.NrOfInstances,
                resizer,
                Pool.DefaultSupervisorStrategy,
                routerDispatcher,
                tailChoppingPool.Within,
                tailChoppingPool.Interval,
                tailChoppingPool.Generic.UsePoolDispatcher);
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, TailChoppingPool value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new Protocol.TailChoppingPool(
                GenericRoutingPoolBuilder(formatterResolver.GetActorSystem(), value),
                value.Within,
                value.Interval
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.TailChoppingPool>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    #endregion

    #region -- ConsistentHashingPool --

    public sealed class ConsistentHashingPoolFormatter : MiscMessageFormatter<ConsistentHashingPool>
    {
        public static readonly IMessagePackFormatter<ConsistentHashingPool> Instance = new ConsistentHashingPoolFormatter();

        private ConsistentHashingPoolFormatter() { }

        public override ConsistentHashingPool Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            var consistentHashingPool = formatter.Deserialize(ref reader, DefaultResolver);

            var rawResizer = consistentHashingPool.Resizer;
            Resizer resizer = rawResizer.NonEmtpy()
                ? (Resizer)formatterResolver.Deserialize(rawResizer)
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

        public override void Serialize(ref MessagePackWriter writer, ref int idx, ConsistentHashingPool value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = GenericRoutingPoolBuilder(formatterResolver.GetActorSystem(), value);

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.GenericRoutingPool>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    #endregion
}
