using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Util.Internal;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Serialization.Formatters
{
    public abstract class SystemMessageFormatter<T> : IMessagePackFormatter<T>
    {
        protected static readonly IFormatterResolver DefaultResolver = MessagePackSerializer.DefaultResolver;
        public abstract T Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver);
        public abstract void Serialize(ref MessagePackWriter writer, ref int idx, T value, IFormatterResolver formatterResolver);

        //
        // ActorRef
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static IActorRef ResolveActorRef(ExtendedActorSystem system, string path)
        {
            if (string.IsNullOrEmpty(path)) { return null; }

            return system.Provider.ResolveActorRef(path);
        }
    }

    public sealed class ActorInitializationExceptionFormatter : ActorInitializationExceptionFormatter<ActorInitializationException>
    {
        public static readonly IMessagePackFormatter<ActorInitializationException> Instance = new ActorInitializationExceptionFormatter();
    }
    public class ActorInitializationExceptionFormatter<TException> : SystemMessageFormatter<TException>
        where TException : ActorInitializationException
    {
        public ActorInitializationExceptionFormatter() { }

        public override TException Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ExceptionData>();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            return (TException)ExceptionSupport.ExceptionFromProto(formatterResolver.GetActorSystem(), protoMessage);
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, TException value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = ExceptionSupport.ExceptionToProto(formatterResolver.GetActorSystem(), value);

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ExceptionData>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgCreateFormatter : SystemMessageFormatter<Create>
    {
        public static readonly IMessagePackFormatter<Create> Instance = new SystemMsgCreateFormatter();

        private SystemMsgCreateFormatter() { }

        public override Create Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.CreateData>();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            return new Create((ActorInitializationException)ExceptionSupport.ExceptionFromProto(
                formatterResolver.GetActorSystem(), protoMessage.Cause));
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, Create value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new Protocol.CreateData(ExceptionSupport.ExceptionToProto(
                formatterResolver.GetActorSystem(),
                value.Failure));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.CreateData>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgRecreateFormatter : SystemMessageFormatter<Recreate>
    {
        public static readonly IMessagePackFormatter<Recreate> Instance = new SystemMsgRecreateFormatter();

        private SystemMsgRecreateFormatter() { }

        public override Recreate Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.RecreateData>();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            return new Recreate(ExceptionSupport.ExceptionFromProto(
                formatterResolver.GetActorSystem(), protoMessage.Cause));
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, Recreate value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new Protocol.RecreateData(ExceptionSupport.ExceptionToProto(
                formatterResolver.GetActorSystem(),
                value.Cause));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.RecreateData>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgSuspendFormatter : SystemMessageFormatter<Suspend>
    {
        public static readonly IMessagePackFormatter<Suspend> Instance = new SystemMsgSuspendFormatter();

        private SystemMsgSuspendFormatter() { }

        public override Suspend Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            reader.Advance(1);
            return new Suspend();
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, Suspend value, IFormatterResolver formatterResolver)
        {
            writer.WriteNil(ref idx); return;
        }
    }

    public sealed class SystemMsgResumeFormatter : SystemMessageFormatter<Resume>
    {
        public static readonly IMessagePackFormatter<Resume> Instance = new SystemMsgResumeFormatter();

        private SystemMsgResumeFormatter() { }

        public override Resume Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ResumeData>();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            return new Resume(ExceptionSupport.ExceptionFromProto(
                formatterResolver.GetActorSystem(), protoMessage.Cause));
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, Resume value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new Protocol.ResumeData(ExceptionSupport.ExceptionToProto(
                formatterResolver.GetActorSystem(),
                value.CausedByFailure));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ResumeData>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgTerminateFormatter : SystemMessageFormatter<Terminate>
    {
        public static readonly IMessagePackFormatter<Terminate> Instance = new SystemMsgTerminateFormatter();

        private SystemMsgTerminateFormatter() { }

        public override Terminate Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            reader.Advance(1);
            return new Terminate();
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, Terminate value, IFormatterResolver formatterResolver)
        {
            writer.WriteNil(ref idx); return;
        }
    }

    public sealed class SystemMsgSuperviseFormatter : SystemMessageFormatter<Supervise>
    {
        public static readonly IMessagePackFormatter<Supervise> Instance = new SystemMsgSuperviseFormatter();

        private SystemMsgSuperviseFormatter() { }

        public override Supervise Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.SuperviseData>();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            return new Supervise(
                ResolveActorRef(formatterResolver.GetActorSystem(), protoMessage.Child.Path),
                protoMessage.Async);
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, Supervise value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new Protocol.SuperviseData(
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Child)),
                value.Async
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.SuperviseData>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgWatchFormatter : SystemMessageFormatter<Watch>
    {
        public static readonly IMessagePackFormatter<Watch> Instance = new SystemMsgWatchFormatter();

        private SystemMsgWatchFormatter() { }

        public override Watch Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.WatchData>();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            var system = formatterResolver.GetActorSystem();
            return new Watch(
                ResolveActorRef(system, protoMessage.Watchee.Path).AsInstanceOf<IInternalActorRef>(),
                ResolveActorRef(system, protoMessage.Watcher.Path).AsInstanceOf<IInternalActorRef>());
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, Watch value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new Protocol.WatchData(
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Watchee)),
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Watcher))
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.WatchData>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgUnwatchFormatter : SystemMessageFormatter<Unwatch>
    {
        public static readonly IMessagePackFormatter<Unwatch> Instance = new SystemMsgUnwatchFormatter();

        private SystemMsgUnwatchFormatter() { }

        public override Unwatch Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.WatchData>();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            var system = formatterResolver.GetActorSystem();
            return new Unwatch(
                ResolveActorRef(system, protoMessage.Watchee.Path).AsInstanceOf<IInternalActorRef>(),
                ResolveActorRef(system, protoMessage.Watcher.Path).AsInstanceOf<IInternalActorRef>());
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, Unwatch value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new Protocol.WatchData(
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Watchee)),
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Watcher))
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.WatchData>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgFailedFormatter : SystemMessageFormatter<Failed>
    {
        public static readonly IMessagePackFormatter<Failed> Instance = new SystemMsgFailedFormatter();

        private SystemMsgFailedFormatter() { }

        public override Failed Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.FailedData>();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            var system = formatterResolver.GetActorSystem();
            return new Failed(
                ResolveActorRef(system, protoMessage.Child.Path),
                ExceptionSupport.ExceptionFromProto(system, protoMessage.Cause),
                (long)protoMessage.Uid);
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, Failed value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new Protocol.FailedData(
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Child)),
                ExceptionSupport.ExceptionToProto(formatterResolver.GetActorSystem(), value.Cause),
                (ulong)value.Uid
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.FailedData>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgDeathWatchNotificationFormatter : SystemMessageFormatter<DeathWatchNotification>
    {
        public static readonly IMessagePackFormatter<DeathWatchNotification> Instance = new SystemMsgDeathWatchNotificationFormatter();

        private SystemMsgDeathWatchNotificationFormatter() { }

        public override DeathWatchNotification Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.DeathWatchNotificationData>();
            var protoMessage = formatter.Deserialize(ref reader, DefaultResolver);

            var system = formatterResolver.GetActorSystem();
            return new DeathWatchNotification(
                system.Provider.ResolveActorRef(protoMessage.Actor.Path),
                protoMessage.ExistenceConfirmed,
                protoMessage.AddressTerminated);
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, DeathWatchNotification value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var protoMessage = new Protocol.DeathWatchNotificationData(
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Actor)),
                value.ExistenceConfirmed,
                value.AddressTerminated
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.DeathWatchNotificationData>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgNoMessageFormatter : SystemMessageFormatter<NoMessage>
    {
        public static readonly IMessagePackFormatter<NoMessage> Instance = new SystemMsgNoMessageFormatter();

        private SystemMsgNoMessageFormatter() { }

        public override NoMessage Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            reader.Advance(1);
            return NoMessage.Instance;
        }

        public override void Serialize(ref MessagePackWriter writer, ref int idx, NoMessage value, IFormatterResolver formatterResolver)
        {
            writer.WriteNil(ref idx); return;
        }
    }
}