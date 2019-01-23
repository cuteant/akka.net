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

namespace Akka.Remote.Serialization
{
    public sealed class SystemMessageSerializer : Akka.Serialization.Serializer
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        private static readonly byte[] EmptyBytes = EmptyArray<byte>.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemMessageSerializer" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public SystemMessageSerializer(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public override bool IncludeManifest { get; } = true;

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case Create create:
                    return CreateToProto(system, create);
                case Recreate recreate:
                    return RecreateToProto(system, recreate);
                case Suspend suspend:
                    return EmptyBytes;
                case Resume resume:
                    return ResumeToProto(system, resume);
                case Terminate terminate:
                    return EmptyBytes;
                case Supervise supervise:
                    return SuperviseToProto(supervise);
                case Watch watch:
                    return WatchToProto(watch);
                case Unwatch unwatch:
                    return UnwatchToProto(unwatch);
                case Failed failed:
                    return FailedToProto(system, failed);
                case DeathWatchNotification deathWatchNotification:
                    return DeathWatchNotificationToProto(deathWatchNotification);
                case NoMessage noMessage:
                    return ThrowHelper.ThrowArgumentException_Serializer_SystemMsg_NoMessage();
                default:
                    return ThrowHelper.ThrowArgumentException_Serializer_S(obj);
            }
        }

        static class _
        {
            internal const int Create = 0;
            internal const int Recreate = 1;
            internal const int Suspend = 2;
            internal const int Resume = 3;
            internal const int Terminate = 4;
            internal const int Supervise = 5;
            internal const int Watch = 6;
            internal const int Unwatch = 7;
            internal const int Failed = 8;
            internal const int DeathWatchNotification = 9;
        }
        private static readonly Dictionary<Type, int> s_fromBinaryMap = new Dictionary<Type, int>()
        {
            { typeof(Create), _.Create },
            { typeof(Recreate), _.Recreate },
            { typeof(Suspend), _.Suspend },
            { typeof(Resume), _.Resume },
            { typeof(Terminate), _.Terminate },
            { typeof(Supervise), _.Supervise },
            { typeof(Watch), _.Watch },
            { typeof(Unwatch), _.Unwatch },
            { typeof(Failed), _.Failed },
            { typeof(DeathWatchNotification), _.DeathWatchNotification },
        };

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, Type type)
        {
            if (s_fromBinaryMap.TryGetValue(type, out var flag))
            {
                switch (flag)
                {
                    case _.Create:
                        return CreateFromProto(system, bytes);
                    case _.Recreate:
                        return RecreateFromProto(system, bytes);
                    case _.Suspend:
                        return new Suspend();
                    case _.Resume:
                        return ResumeFromProto(system, bytes);
                    case _.Terminate:
                        return new Terminate();
                    case _.Supervise:
                        return SuperviseFromProto(system, bytes);
                    case _.Watch:
                        return WatchFromProto(system, bytes);
                    case _.Unwatch:
                        return UnwatchFromProto(system, bytes);
                    case _.Failed:
                        return FailedFromProto(system, bytes);
                    case _.DeathWatchNotification:
                        return DeathWatchNotificationFromProto(system, bytes);
                }
            }
            return ThrowHelper.ThrowArgumentException_Serializer_SystemMsg(type);
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
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(supervise.Child)),
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
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(watch.Watchee)),
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(watch.Watcher))
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
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(unwatch.Watchee)),
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(unwatch.Watcher))
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
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(failed.Child)),
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
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(deathWatchNotification.Actor)),
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
    }
}
