//-----------------------------------------------------------------------
// <copyright file="SystemMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Util.Internal;
using CuteAnt;
using MessagePack;

namespace Akka.Serialization
{
    public sealed class SystemMessageSerializer : SerializerWithIntegerManifest
    {
        #region manifests

        static class _
        {
            internal const int CreateManifest = 100;
            internal const int RecreateManifest = 101;
            internal const int SuspendManifest = 102;
            internal const int ResumeManifest = 103;
            internal const int TerminateManifest = 104;
            internal const int SuperviseManifest = 105;
            internal const int WatchManifest = 106;
            internal const int UnwatchManifest = 107;
            internal const int FailedManifest = 108;
            internal const int DeathWatchNotificationManifest = 109;
        }

        private static readonly Dictionary<Type, int> ManifestMap;

        static SystemMessageSerializer()
        {
            ManifestMap = new Dictionary<Type, int>()
            {
                { typeof(Create), _.CreateManifest },
                { typeof(Recreate), _.RecreateManifest },
                { typeof(Suspend), _.SuspendManifest },
                { typeof(Resume), _.ResumeManifest },
                { typeof(Terminate), _.TerminateManifest },
                { typeof(Supervise), _.SuperviseManifest },
                { typeof(Watch), _.WatchManifest },
                { typeof(Unwatch), _.UnwatchManifest },
                { typeof(Failed), _.FailedManifest },
                { typeof(DeathWatchNotification), _.DeathWatchNotificationManifest },
            };
        }

        #endregion

        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        private static readonly byte[] EmptyBytes = EmptyArray<byte>.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemMessageSerializer" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public SystemMessageSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj, out int manifest)
        {
            switch (obj)
            {
                case Create create:
                    manifest = _.CreateManifest;
                    return CreateToProto(system, create);
                case Recreate recreate:
                    manifest = _.RecreateManifest;
                    return RecreateToProto(system, recreate);
                case Suspend suspend:
                    manifest = _.SuspendManifest;
                    return EmptyBytes;
                case Resume resume:
                    manifest = _.ResumeManifest;
                    return ResumeToProto(system, resume);
                case Terminate terminate:
                    manifest = _.TerminateManifest;
                    return EmptyBytes;
                case Supervise supervise:
                    manifest = _.SuperviseManifest;
                    return SuperviseToProto(supervise);
                case Watch watch:
                    manifest = _.WatchManifest;
                    return WatchToProto(watch);
                case Unwatch unwatch:
                    manifest = _.UnwatchManifest;
                    return UnwatchToProto(unwatch);
                case Failed failed:
                    manifest = _.FailedManifest;
                    return FailedToProto(system, failed);
                case DeathWatchNotification deathWatchNotification:
                    manifest = _.DeathWatchNotificationManifest;
                    return DeathWatchNotificationToProto(deathWatchNotification);
                case NoMessage noMessage:
                    manifest = 0;
                    AkkaThrowHelper.ThrowArgumentException_Serializer_SystemMsg_NoMessage(); return null;
                default:
                    manifest = 0; AkkaThrowHelper.ThrowArgumentException_Serializer_S(obj); return null;
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, int manifest)
        {
            switch (manifest)
            {
                case _.CreateManifest:
                    return CreateFromProto(system, bytes);
                case _.RecreateManifest:
                    return RecreateFromProto(system, bytes);
                case _.SuspendManifest:
                    return new Suspend();
                case _.ResumeManifest:
                    return ResumeFromProto(system, bytes);
                case _.TerminateManifest:
                    return new Terminate();
                case _.SuperviseManifest:
                    return SuperviseFromProto(system, bytes);
                case _.WatchManifest:
                    return WatchFromProto(system, bytes);
                case _.UnwatchManifest:
                    return UnwatchFromProto(system, bytes);
                case _.FailedManifest:
                    return FailedFromProto(system, bytes);
                case _.DeathWatchNotificationManifest:
                    return DeathWatchNotificationFromProto(system, bytes);
            }
            ThrowArgumentException_Serializer_SystemMsg(manifest); return null;
        }

        /// <inheritdoc />
        protected override int GetManifest(Type type)
        {
            if (null == type) { return 0; }
            if (ManifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            AkkaThrowHelper.ThrowArgumentException_Serializer_D(type); return 0;
        }

        /// <inheritdoc />
        public override int Manifest(object o)
        {
            switch (o)
            {
                case Create create:
                    return _.CreateManifest;
                case Recreate recreate:
                    return _.RecreateManifest;
                case Suspend suspend:
                    return _.SuspendManifest;
                case Resume resume:
                    return _.ResumeManifest;
                case Terminate terminate:
                    return _.TerminateManifest;
                case Supervise supervise:
                    return _.SuperviseManifest;
                case Watch watch:
                    return _.WatchManifest;
                case Unwatch unwatch:
                    return _.UnwatchManifest;
                case Failed failed:
                    return _.FailedManifest;
                case DeathWatchNotification deathWatchNotification:
                    return _.DeathWatchNotificationManifest;
                case NoMessage noMessage:
                    AkkaThrowHelper.ThrowArgumentException_Serializer_SystemMsg_NoMessage(); return 0;
                default:
                    AkkaThrowHelper.ThrowArgumentException_Serializer_D(o); return 0;
            }
        }

        //
        // Create
        //
        internal static byte[] CreateToProto(ExtendedActorSystem system, Create create)
        {
            var message = new Protocol.CreateData(ExceptionSupport.ExceptionToProto(system, create.Failure));
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        internal static Create CreateFromProto(ExtendedActorSystem system, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.CreateData>(bytes, s_defaultResolver);
            var payload = (ActorInitializationException)ExceptionSupport.ExceptionFromProto(system, proto.Cause);
            return new Create(payload);
        }

        //
        // Recreate
        //
        internal static byte[] RecreateToProto(ExtendedActorSystem system, Recreate recreate)
        {
            var message = new Protocol.RecreateData(ExceptionSupport.ExceptionToProto(system, recreate.Cause));
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        internal static Recreate RecreateFromProto(ExtendedActorSystem system, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.RecreateData>(bytes, s_defaultResolver);
            var payload = ExceptionSupport.ExceptionFromProto(system, proto.Cause);
            return new Recreate(payload);
        }

        //
        // Recreate
        //
        internal static byte[] ResumeToProto(ExtendedActorSystem system, Resume resume)
        {
            var message = new Protocol.ResumeData(ExceptionSupport.ExceptionToProto(system, resume.CausedByFailure));
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        internal static Resume ResumeFromProto(ExtendedActorSystem system, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.ResumeData>(bytes, s_defaultResolver);
            var payload = ExceptionSupport.ExceptionFromProto(system, proto.Cause);
            return new Resume(payload);
        }

        //
        // Supervise
        //
        internal static byte[] SuperviseToProto(Supervise supervise)
        {
            var message = new Protocol.SuperviseData(
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(supervise.Child)),
                supervise.Async
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        internal static Supervise SuperviseFromProto(ExtendedActorSystem system, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.SuperviseData>(bytes, s_defaultResolver);
            return new Supervise(ResolveActorRef(system, proto.Child.Path), proto.Async);
        }

        //
        // Watch
        //
        internal static byte[] WatchToProto(Watch watch)
        {
            var message = new Protocol.WatchData(
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(watch.Watchee)),
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(watch.Watcher))
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        internal static Watch WatchFromProto(ExtendedActorSystem system, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.WatchData>(bytes, s_defaultResolver);
            return new Watch(
                ResolveActorRef(system, proto.Watchee.Path).AsInstanceOf<IInternalActorRef>(),
                ResolveActorRef(system, proto.Watcher.Path).AsInstanceOf<IInternalActorRef>());
        }

        //
        // Unwatch
        //
        internal static byte[] UnwatchToProto(Unwatch unwatch)
        {
            var message = new Protocol.WatchData(
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(unwatch.Watchee)),
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(unwatch.Watcher))
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        internal static Unwatch UnwatchFromProto(ExtendedActorSystem system, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.WatchData>(bytes, s_defaultResolver);
            return new Unwatch(
                ResolveActorRef(system, proto.Watchee.Path).AsInstanceOf<IInternalActorRef>(),
                ResolveActorRef(system, proto.Watcher.Path).AsInstanceOf<IInternalActorRef>());
        }

        //
        // Failed
        //
        internal static byte[] FailedToProto(ExtendedActorSystem system, Failed failed)
        {
            var message = new Protocol.FailedData(
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(failed.Child)),
                ExceptionSupport.ExceptionToProto(system, failed.Cause),
                (ulong)failed.Uid
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        internal static Failed FailedFromProto(ExtendedActorSystem system, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.FailedData>(bytes, s_defaultResolver);

            return new Failed(
                ResolveActorRef(system, proto.Child.Path),
                ExceptionSupport.ExceptionFromProto(system, proto.Cause),
                (long)proto.Uid);
        }

        //
        // DeathWatchNotification
        //
        internal static byte[] DeathWatchNotificationToProto(DeathWatchNotification deathWatchNotification)
        {
            var message = new Protocol.DeathWatchNotificationData(
                new Protocol.ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(deathWatchNotification.Actor)),
                deathWatchNotification.ExistenceConfirmed,
                deathWatchNotification.AddressTerminated
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        internal static DeathWatchNotification DeathWatchNotificationFromProto(ExtendedActorSystem system, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.DeathWatchNotificationData>(bytes, s_defaultResolver);

            return new DeathWatchNotification(
                system.Provider.ResolveActorRef(proto.Actor.Path),
                proto.ExistenceConfirmed,
                proto.AddressTerminated);
        }

        //
        // ActorRef
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IActorRef ResolveActorRef(ExtendedActorSystem system, string path)
        {
            if (string.IsNullOrEmpty(path)) { return null; }

            return system.Provider.ResolveActorRef(path);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentException_Serializer_SystemMsg(int manifest)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in [${nameof(SystemMessageSerializer)}]");
            }
        }
    }
}
