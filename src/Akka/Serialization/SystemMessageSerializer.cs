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
    public sealed class SystemMessageSerializer : SerializerWithStringManifest
    {
        #region manifests

        static class Manifests
        {
            internal const string CreateManifest = "C";
            internal const string RecreateManifest = "RC";
            internal const string SuspendManifest = "S";
            internal const string ResumeManifest = "R";
            internal const string TerminateManifest = "T";
            internal const string SuperviseManifest = "SV";
            internal const string WatchManifest = "W";
            internal const string UnwatchManifest = "UW";
            internal const string FailedManifest = "F";
            internal const string DeathWatchNotificationManifest = "DWN";
        }

        private static readonly Dictionary<Type, string> ManifestMap;

        static SystemMessageSerializer()
        {
            ManifestMap = new Dictionary<Type, string>()
            {
                { typeof(Create), Manifests.CreateManifest },
                { typeof(Recreate), Manifests.RecreateManifest },
                { typeof(Suspend), Manifests.SuspendManifest },
                { typeof(Resume), Manifests.ResumeManifest },
                { typeof(Terminate), Manifests.TerminateManifest },
                { typeof(Supervise), Manifests.SuperviseManifest },
                { typeof(Watch), Manifests.WatchManifest },
                { typeof(Unwatch), Manifests.UnwatchManifest },
                { typeof(Failed), Manifests.FailedManifest },
                { typeof(DeathWatchNotification), Manifests.DeathWatchNotificationManifest },
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
        public override byte[] ToBinary(object obj, out string manifest)
        {
            switch (obj)
            {
                case Create create:
                    manifest = Manifests.CreateManifest;
                    return CreateToProto(_system, create);
                case Recreate recreate:
                    manifest = Manifests.RecreateManifest;
                    return RecreateToProto(_system, recreate);
                case Suspend _:
                    manifest = Manifests.SuspendManifest;
                    return EmptyBytes;
                case Resume resume:
                    manifest = Manifests.ResumeManifest;
                    return ResumeToProto(_system, resume);
                case Terminate _:
                    manifest = Manifests.TerminateManifest;
                    return EmptyBytes;
                case Supervise supervise:
                    manifest = Manifests.SuperviseManifest;
                    return SuperviseToProto(supervise);
                case Watch watch:
                    manifest = Manifests.WatchManifest;
                    return WatchToProto(watch);
                case Unwatch unwatch:
                    manifest = Manifests.UnwatchManifest;
                    return UnwatchToProto(unwatch);
                case Failed failed:
                    manifest = Manifests.FailedManifest;
                    return FailedToProto(_system, failed);
                case DeathWatchNotification deathWatchNotification:
                    manifest = Manifests.DeathWatchNotificationManifest;
                    return DeathWatchNotificationToProto(deathWatchNotification);
                case NoMessage _:
                    throw AkkaThrowHelper.GetArgumentException_Serializer_SystemMsg_NoMessage();
                default:
                    throw AkkaThrowHelper.GetArgumentException_Serializer_S(obj);
            }
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, string manifest)
        {
            switch (manifest)
            {
                case Manifests.CreateManifest:
                    return CreateFromProto(_system, bytes);
                case Manifests.RecreateManifest:
                    return RecreateFromProto(_system, bytes);
                case Manifests.SuspendManifest:
                    return new Suspend();
                case Manifests.ResumeManifest:
                    return ResumeFromProto(_system, bytes);
                case Manifests.TerminateManifest:
                    return new Terminate();
                case Manifests.SuperviseManifest:
                    return SuperviseFromProto(_system, bytes);
                case Manifests.WatchManifest:
                    return WatchFromProto(_system, bytes);
                case Manifests.UnwatchManifest:
                    return UnwatchFromProto(_system, bytes);
                case Manifests.FailedManifest:
                    return FailedFromProto(_system, bytes);
                case Manifests.DeathWatchNotificationManifest:
                    return DeathWatchNotificationFromProto(_system, bytes);
            }
            throw GetArgumentException_Serializer_SystemMsg(manifest);
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (null == type) { return null; }
            if (ManifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            throw AkkaThrowHelper.GetArgumentException_Serializer_D(type);
        }

        /// <inheritdoc />
        public override string Manifest(object o)
        {
            switch (o)
            {
                case Create _:
                    return Manifests.CreateManifest;
                case Recreate _:
                    return Manifests.RecreateManifest;
                case Suspend _:
                    return Manifests.SuspendManifest;
                case Resume _:
                    return Manifests.ResumeManifest;
                case Terminate _:
                    return Manifests.TerminateManifest;
                case Supervise _:
                    return Manifests.SuperviseManifest;
                case Watch _:
                    return Manifests.WatchManifest;
                case Unwatch _:
                    return Manifests.UnwatchManifest;
                case Failed _:
                    return Manifests.FailedManifest;
                case DeathWatchNotification _:
                    return Manifests.DeathWatchNotificationManifest;
                case NoMessage _:
                    throw AkkaThrowHelper.GetArgumentException_Serializer_SystemMsg_NoMessage();
                default:
                    throw AkkaThrowHelper.GetArgumentException_Serializer_D(o);
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

        internal static Create CreateFromProto(ExtendedActorSystem system, in ReadOnlySpan<byte> bytes)
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

        internal static Recreate RecreateFromProto(ExtendedActorSystem system, in ReadOnlySpan<byte> bytes)
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

        internal static Resume ResumeFromProto(ExtendedActorSystem system, in ReadOnlySpan<byte> bytes)
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

        internal static Supervise SuperviseFromProto(ExtendedActorSystem system, in ReadOnlySpan<byte> bytes)
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

        internal static Watch WatchFromProto(ExtendedActorSystem system, in ReadOnlySpan<byte> bytes)
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

        internal static Unwatch UnwatchFromProto(ExtendedActorSystem system, in ReadOnlySpan<byte> bytes)
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

        internal static Failed FailedFromProto(ExtendedActorSystem system, in ReadOnlySpan<byte> bytes)
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

        internal static DeathWatchNotification DeathWatchNotificationFromProto(ExtendedActorSystem system, in ReadOnlySpan<byte> bytes)
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
        private static ArgumentException GetArgumentException_Serializer_SystemMsg(string manifest)
        {
            return new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in [${nameof(SystemMessageSerializer)}]");
        }
    }
}
