//-----------------------------------------------------------------------
// <copyright file="SystemMessageSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using Akka.Actor;
using Akka.Dispatch.SysMsg;
using Akka.Util;
using Akka.Util.Internal;
using CuteAnt;
using MessagePack;

namespace Akka.Remote.Serialization
{
    public sealed class SystemMessageSerializer : Akka.Serialization.Serializer
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        private ExceptionSupport _exceptionSupport;

        private static readonly byte[] EmptyBytes = EmptyArray<byte>.Instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemMessageSerializer" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public SystemMessageSerializer(ExtendedActorSystem system) : base(system)
        {
            _exceptionSupport = new ExceptionSupport(system);
        }

        /// <inheritdoc />
        public override bool IncludeManifest { get; } = true;

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            switch (obj)
            {
                case Create create:
                    return CreateToProto(create);
                case Recreate recreate:
                    return RecreateToProto(recreate);
                case Suspend suspend:
                    return EmptyBytes;
                case Resume resume:
                    return ResumeToProto(resume);
                case Terminate terminate:
                    return EmptyBytes;
                case Supervise supervise:
                    return SuperviseToProto(supervise);
                case Watch watch:
                    return WatchToProto(watch);
                case Unwatch unwatch:
                    return UnwatchToProto(unwatch);
                case Failed failed:
                    return FailedToProto(failed);
                case DeathWatchNotification deathWatchNotification:
                    return DeathWatchNotificationToProto(deathWatchNotification);
                case NoMessage noMessage:
                    throw new ArgumentException("NoMessage should never be serialized or deserialized");
                default:
                    throw new ArgumentException($"Cannot serialize object of type [{obj.GetType().TypeQualifiedName()}]");
            }
        }

        private static readonly Dictionary<Type, Func<SystemMessageSerializer, byte[], object>> s_fromBinaryMap = new Dictionary<Type, Func<SystemMessageSerializer, byte[], object>>()
        {
            { typeof(Create), (s, b)=> CreateFromProto(s, b) },
            { typeof(Recreate), (s, b)=> RecreateFromProto(s, b) },
            { typeof(Suspend), (s, b)=> new Suspend() },
            { typeof(Resume), (s, b)=> ResumeFromProto(s, b) },
            { typeof(Terminate), (s, b)=> new Terminate() },
            { typeof(Supervise), (s, b)=> SuperviseFromProto(s, b) },
            { typeof(Watch), (s, b)=> WatchFromProto(s, b) },
            { typeof(Unwatch), (s, b)=> UnwatchFromProto(s, b) },
            { typeof(Failed), (s, b)=> FailedFromProto(s, b) },
            { typeof(DeathWatchNotification), (s, b)=> DeathWatchNotificationFromProto(s, b) },
        };

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, Type type)
        {
            if (s_fromBinaryMap.TryGetValue(type, out var factory))
            {
                return factory(this, bytes);
            }

            throw new ArgumentException($"Unimplemented deserialization of message with manifest [{type.TypeQualifiedName()}] in [${nameof(SystemMessageSerializer)}]");
        }

        //
        // Create
        //
        private byte[] CreateToProto(Create create)
        {
            var message = new Protocol.CreateData(_exceptionSupport.ExceptionToProto(create.Failure));
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static Create CreateFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.CreateData>(bytes, s_defaultResolver);
            var payload = (ActorInitializationException)serializer._exceptionSupport.ExceptionFromProto(proto.Cause);
            return new Create(payload);
        }

        //
        // Recreate
        //
        private byte[] RecreateToProto(Recreate recreate)
        {
            var message = new Protocol.RecreateData(_exceptionSupport.ExceptionToProto(recreate.Cause));
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static Recreate RecreateFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.RecreateData>(bytes, s_defaultResolver);
            var payload = serializer._exceptionSupport.ExceptionFromProto(proto.Cause);
            return new Recreate(payload);
        }

        //
        // Recreate
        //
        private byte[] ResumeToProto(Resume resume)
        {
            var message = new Protocol.ResumeData(_exceptionSupport.ExceptionToProto(resume.CausedByFailure));
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static Resume ResumeFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.ResumeData>(bytes, s_defaultResolver);
            var payload = serializer._exceptionSupport.ExceptionFromProto(proto.Cause);
            return new Resume(payload);
        }

        //
        // Supervise
        //
        private static byte[] SuperviseToProto(Supervise supervise)
        {
            var message = new Protocol.SuperviseData(
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(supervise.Child)),
                supervise.Async
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static Supervise SuperviseFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.SuperviseData>(bytes, s_defaultResolver);
            return new Supervise(serializer.ResolveActorRef(proto.Child.Path), proto.Async);
        }

        //
        // Watch
        //
        private static byte[] WatchToProto(Watch watch)
        {
            var message = new Protocol.WatchData(
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(watch.Watchee)),
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(watch.Watcher))
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static Watch WatchFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.WatchData>(bytes, s_defaultResolver);
            return new Watch(
                serializer.ResolveActorRef(proto.Watchee.Path).AsInstanceOf<IInternalActorRef>(),
                serializer.ResolveActorRef(proto.Watcher.Path).AsInstanceOf<IInternalActorRef>());
        }

        //
        // Unwatch
        //
        private static byte[] UnwatchToProto(Unwatch unwatch)
        {
            var message = new Protocol.WatchData(
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(unwatch.Watchee)),
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(unwatch.Watcher))
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static Unwatch UnwatchFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.WatchData>(bytes, s_defaultResolver);
            return new Unwatch(
                serializer.ResolveActorRef(proto.Watchee.Path).AsInstanceOf<IInternalActorRef>(),
                serializer.ResolveActorRef(proto.Watcher.Path).AsInstanceOf<IInternalActorRef>());
        }

        //
        // Failed
        //
        private byte[] FailedToProto(Failed failed)
        {
            var message = new Protocol.FailedData(
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(failed.Child)),
                _exceptionSupport.ExceptionToProto(failed.Cause),
                (ulong)failed.Uid
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static Failed FailedFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.FailedData>(bytes, s_defaultResolver);

            return new Failed(
                serializer.ResolveActorRef(proto.Child.Path),
                serializer._exceptionSupport.ExceptionFromProto(proto.Cause),
                (long)proto.Uid);
        }

        //
        // DeathWatchNotification
        //
        private static byte[] DeathWatchNotificationToProto(DeathWatchNotification deathWatchNotification)
        {
            var message = new Protocol.DeathWatchNotificationData(
                new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(deathWatchNotification.Actor)),
                deathWatchNotification.ExistenceConfirmed,
                deathWatchNotification.AddressTerminated
            );
            return MessagePackSerializer.Serialize(message, s_defaultResolver);
        }

        private static DeathWatchNotification DeathWatchNotificationFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.DeathWatchNotificationData>(bytes, s_defaultResolver);

            return new DeathWatchNotification(
                serializer.system.Provider.ResolveActorRef(proto.Actor.Path),
                proto.ExistenceConfirmed,
                proto.AddressTerminated);
        }

        //
        // ActorRef
        //
        private IActorRef ResolveActorRef(string path)
        {
            if (string.IsNullOrEmpty(path)) { return null; }

            return system.Provider.ResolveActorRef(path);
        }
    }
}
