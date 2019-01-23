using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Serialization;
using Akka.Util.Internal;
using MessagePack;
using MessagePack.Formatters;

namespace Akka.Remote.Serialization.Formatters
{
    public abstract class SystemMessageFormatter<T> : IMessagePackFormatter<T>
    {
        protected static readonly IFormatterResolver DefaultResolver = MessagePackSerializer.DefaultResolver;
        public abstract T Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize);
        public abstract int Serialize(ref byte[] bytes, int offset, T value, IFormatterResolver formatterResolver);
    }

    public sealed class ActorInitializationExceptionFormatter: ActorInitializationExceptionFormatter<ActorInitializationException>
    {
        public static readonly IMessagePackFormatter<ActorInitializationException> Instance = new ActorInitializationExceptionFormatter();
    }
    public class ActorInitializationExceptionFormatter<TException> : SystemMessageFormatter<TException>
        where TException : ActorInitializationException
    {
        public ActorInitializationExceptionFormatter() { }

        public override TException Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ExceptionData>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            return (TException)ExceptionSupport.ExceptionFromProto(formatterResolver.GetActorSystem(), protoMessage);
        }

        public override int Serialize(ref byte[] bytes, int offset, TException value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = ExceptionSupport.ExceptionToProto(formatterResolver.GetActorSystem(), value);

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ExceptionData>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgCreateFormatter : SystemMessageFormatter<Create>
    {
        public static readonly IMessagePackFormatter<Create> Instance = new SystemMsgCreateFormatter();

        private SystemMsgCreateFormatter() { }

        public override Create Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.CreateData>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            return new Create((ActorInitializationException)ExceptionSupport.ExceptionFromProto(
                formatterResolver.GetActorSystem(), protoMessage.Cause));
        }

        public override int Serialize(ref byte[] bytes, int offset, Create value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.CreateData(ExceptionSupport.ExceptionToProto(
                formatterResolver.GetActorSystem(),
                value.Failure));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.CreateData>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgRecreateFormatter : SystemMessageFormatter<Recreate>
    {
        public static readonly IMessagePackFormatter<Recreate> Instance = new SystemMsgRecreateFormatter();

        private SystemMsgRecreateFormatter() { }

        public override Recreate Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.RecreateData>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            return new Recreate((ActorInitializationException)ExceptionSupport.ExceptionFromProto(
                formatterResolver.GetActorSystem(), protoMessage.Cause));
        }

        public override int Serialize(ref byte[] bytes, int offset, Recreate value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.RecreateData(ExceptionSupport.ExceptionToProto(
                formatterResolver.GetActorSystem(),
                value.Cause));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.RecreateData>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgSuspendFormatter : SystemMessageFormatter<Suspend>
    {
        public static readonly IMessagePackFormatter<Suspend> Instance = new SystemMsgSuspendFormatter();

        private SystemMsgSuspendFormatter() { }

        public override Suspend Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            readSize = 1;
            return new Suspend();
        }

        public override int Serialize(ref byte[] bytes, int offset, Suspend value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteNil(ref bytes, offset);
        }
    }

    public sealed class SystemMsgResumeFormatter : SystemMessageFormatter<Resume>
    {
        public static readonly IMessagePackFormatter<Resume> Instance = new SystemMsgResumeFormatter();

        private SystemMsgResumeFormatter() { }

        public override Resume Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ResumeData>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            return new Resume((ActorInitializationException)ExceptionSupport.ExceptionFromProto(
                formatterResolver.GetActorSystem(), protoMessage.Cause));
        }

        public override int Serialize(ref byte[] bytes, int offset, Resume value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.ResumeData(ExceptionSupport.ExceptionToProto(
                formatterResolver.GetActorSystem(),
                value.CausedByFailure));

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.ResumeData>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgTerminateFormatter : SystemMessageFormatter<Terminate>
    {
        public static readonly IMessagePackFormatter<Terminate> Instance = new SystemMsgTerminateFormatter();

        private SystemMsgTerminateFormatter() { }

        public override Terminate Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            readSize = 1;
            return new Terminate();
        }

        public override int Serialize(ref byte[] bytes, int offset, Terminate value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteNil(ref bytes, offset);
        }
    }

    public sealed class SystemMsgSuperviseFormatter : SystemMessageFormatter<Supervise>
    {
        public static readonly IMessagePackFormatter<Supervise> Instance = new SystemMsgSuperviseFormatter();

        private SystemMsgSuperviseFormatter() { }

        public override Supervise Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.SuperviseData>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            return new Supervise(
                SystemMessageSerializer.ResolveActorRef(formatterResolver.GetActorSystem(), protoMessage.Child.Path),
                protoMessage.Async);
        }

        public override int Serialize(ref byte[] bytes, int offset, Supervise value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.SuperviseData(
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Child)),
                value.Async
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.SuperviseData>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgWatchFormatter : SystemMessageFormatter<Watch>
    {
        public static readonly IMessagePackFormatter<Watch> Instance = new SystemMsgWatchFormatter();

        private SystemMsgWatchFormatter() { }

        public override Watch Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.WatchData>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var system = formatterResolver.GetActorSystem();
            return new Watch(
                SystemMessageSerializer.ResolveActorRef(system, protoMessage.Watchee.Path).AsInstanceOf<IInternalActorRef>(),
                SystemMessageSerializer.ResolveActorRef(system, protoMessage.Watcher.Path).AsInstanceOf<IInternalActorRef>());
        }

        public override int Serialize(ref byte[] bytes, int offset, Watch value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.WatchData(
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Watchee)),
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Watcher))
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.WatchData>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgUnwatchFormatter : SystemMessageFormatter<Unwatch>
    {
        public static readonly IMessagePackFormatter<Unwatch> Instance = new SystemMsgUnwatchFormatter();

        private SystemMsgUnwatchFormatter() { }

        public override Unwatch Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.WatchData>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var system = formatterResolver.GetActorSystem();
            return new Unwatch(
                SystemMessageSerializer.ResolveActorRef(system, protoMessage.Watchee.Path).AsInstanceOf<IInternalActorRef>(),
                SystemMessageSerializer.ResolveActorRef(system, protoMessage.Watcher.Path).AsInstanceOf<IInternalActorRef>());
        }

        public override int Serialize(ref byte[] bytes, int offset, Unwatch value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.WatchData(
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Watchee)),
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Watcher))
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.WatchData>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgFailedFormatter : SystemMessageFormatter<Failed>
    {
        public static readonly IMessagePackFormatter<Failed> Instance = new SystemMsgFailedFormatter();

        private SystemMsgFailedFormatter() { }

        public override Failed Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.FailedData>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var system = formatterResolver.GetActorSystem();
            return new Failed(
                SystemMessageSerializer.ResolveActorRef(system, protoMessage.Child.Path),
                ExceptionSupport.ExceptionFromProto(system, protoMessage.Cause),
                (long)protoMessage.Uid);
        }

        public override int Serialize(ref byte[] bytes, int offset, Failed value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.FailedData(
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Child)),
                ExceptionSupport.ExceptionToProto(formatterResolver.GetActorSystem(), value.Cause),
                (ulong)value.Uid
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.FailedData>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgDeathWatchNotificationFormatter : SystemMessageFormatter<DeathWatchNotification>
    {
        public static readonly IMessagePackFormatter<DeathWatchNotification> Instance = new SystemMsgDeathWatchNotificationFormatter();

        private SystemMsgDeathWatchNotificationFormatter() { }

        public override DeathWatchNotification Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            if (MessagePackBinary.IsNil(bytes, offset)) { readSize = 1; return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.DeathWatchNotificationData>();
            var protoMessage = formatter.Deserialize(bytes, offset, DefaultResolver, out readSize);

            var system = formatterResolver.GetActorSystem();
            return new DeathWatchNotification(
                system.Provider.ResolveActorRef(protoMessage.Actor.Path),
                protoMessage.ExistenceConfirmed,
                protoMessage.AddressTerminated);
        }

        public override int Serialize(ref byte[] bytes, int offset, DeathWatchNotification value, IFormatterResolver formatterResolver)
        {
            if (value == null) { return MessagePackBinary.WriteNil(ref bytes, offset); }

            var protoMessage = new Protocol.DeathWatchNotificationData(
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(value.Actor)),
                value.ExistenceConfirmed,
                value.AddressTerminated
            );

            var formatter = formatterResolver.GetFormatterWithVerify<Protocol.DeathWatchNotificationData>();
            return formatter.Serialize(ref bytes, offset, protoMessage, DefaultResolver);
        }
    }

    public sealed class SystemMsgNoMessageFormatter : SystemMessageFormatter<NoMessage>
    {
        public static readonly IMessagePackFormatter<NoMessage> Instance = new SystemMsgNoMessageFormatter();

        private SystemMsgNoMessageFormatter() { }

        public override NoMessage Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize)
        {
            readSize = 1;
            return NoMessage.Instance;
        }

        public override int Serialize(ref byte[] bytes, int offset, NoMessage value, IFormatterResolver formatterResolver)
        {
            return MessagePackBinary.WriteNil(ref bytes, offset);
        }
    }
}