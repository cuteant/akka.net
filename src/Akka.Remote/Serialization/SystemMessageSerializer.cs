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
using Akka.Serialization;
using Akka.Util;
using Akka.Util.Internal;
using CuteAnt;
using CuteAnt.Extensions.Serialization;

namespace Akka.Remote.Serialization
{
    public sealed class SystemMessageSerializer : Serializer
    {
        private readonly WrappedPayloadSupport _payloadSupport;
        private ExceptionSupport _exceptionSupport;

        private static readonly byte[] EmptyBytes = EmptyArray<byte>.Instance;

        private static readonly MessagePackMessageFormatter s_formatter = MessagePackMessageFormatter.DefaultInstance;
        private const int c_initialBufferSize = 1024 * 2;

        /// <summary>
        /// Initializes a new instance of the <see cref="SystemMessageSerializer" /> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public SystemMessageSerializer(ExtendedActorSystem system) : base(system)
        {
            _payloadSupport = new WrappedPayloadSupport(system);
            _exceptionSupport = new ExceptionSupport(system);
        }

        /// <inheritdoc />
        public override bool IncludeManifest { get; } = true;

        private static readonly Dictionary<Type, bool> s_toBinaryMap = new Dictionary<Type, bool>()
        {
            { typeof(Create), true },
            { typeof(Recreate), true },
            { typeof(Suspend), true },
            { typeof(Resume), true },
            { typeof(Terminate), true },
            { typeof(Supervise), true },
            { typeof(Watch), true },
            { typeof(Unwatch), true },
            { typeof(Failed), true },
            { typeof(DeathWatchNotification), true },
            { typeof(NoMessage), false },
        };

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            //if (s_toBinaryMap.TryGetValue(obj.GetType(), out var canSerialize))
            //{
            //    if (canSerialize)
            //    {
            //        s_formatter.SerializeObject(obj, c_initialBufferSize);
            //    }
            //    else
            //    {
            //        throw new ArgumentException("NoMessage should never be serialized or deserialized");
            //    }
            //}
            //throw new ArgumentException($"Cannot serialize object of type [{obj.GetType().TypeQualifiedName()}]");
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
            //return s_formatter.Deserialize(type, bytes);
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
            var message = new Proto.Msg.CreateData
            {
                Cause = _exceptionSupport.ExceptionToProto(create.Failure)
            };
            return message.ToArray();
        }

        private static Create CreateFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = Proto.Msg.CreateData.Parser.ParseFrom(bytes);
            var payload = (ActorInitializationException)serializer._exceptionSupport.ExceptionFromProto(proto.Cause);
            return new Create(payload);
        }

        //
        // Recreate
        //
        private byte[] RecreateToProto(Recreate recreate)
        {
            var message = new Proto.Msg.RecreateData
            {
                Cause = _exceptionSupport.ExceptionToProto(recreate.Cause)
            };
            return message.ToArray();
        }

        private static Recreate RecreateFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = Proto.Msg.RecreateData.Parser.ParseFrom(bytes);
            var payload = serializer._exceptionSupport.ExceptionFromProto(proto.Cause);
            return new Recreate(payload);
        }

        //
        // Recreate
        //
        private byte[] ResumeToProto(Resume resume)
        {
            var message = new Proto.Msg.ResumeData
            {
                Cause = _exceptionSupport.ExceptionToProto(resume.CausedByFailure)
            };
            return message.ToArray();
        }

        private static Resume ResumeFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = Proto.Msg.ResumeData.Parser.ParseFrom(bytes);
            var payload = serializer._exceptionSupport.ExceptionFromProto(proto.Cause);
            return new Resume(payload);
        }

        //
        // Supervise
        //
        private static byte[] SuperviseToProto(Supervise supervise)
        {
            var message = new Proto.Msg.SuperviseData
            {
                Child = new Proto.Msg.ActorRefData
                {
                    Path = Akka.Serialization.Serialization.SerializedActorPath(supervise.Child)
                },
                Async = supervise.Async
            };
            return message.ToArray();
        }

        private static Supervise SuperviseFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = Proto.Msg.SuperviseData.Parser.ParseFrom(bytes);
            return new Supervise(serializer.ResolveActorRef(proto.Child.Path), proto.Async);
        }

        //
        // Watch
        //
        private static byte[] WatchToProto(Watch watch)
        {
            var message = new Proto.Msg.WatchData
            {
                Watchee = new Proto.Msg.ActorRefData
                {
                    Path = Akka.Serialization.Serialization.SerializedActorPath(watch.Watchee)
                },
                Watcher = new Proto.Msg.ActorRefData
                {
                    Path = Akka.Serialization.Serialization.SerializedActorPath(watch.Watcher)
                }
            };
            return message.ToArray();
        }

        private static Watch WatchFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = Proto.Msg.WatchData.Parser.ParseFrom(bytes);
            return new Watch(
                serializer.ResolveActorRef(proto.Watchee.Path).AsInstanceOf<IInternalActorRef>(),
                serializer.ResolveActorRef(proto.Watcher.Path).AsInstanceOf<IInternalActorRef>());
        }

        //
        // Unwatch
        //
        private static byte[] UnwatchToProto(Unwatch unwatch)
        {
            var message = new Proto.Msg.WatchData
            {
                Watchee = new Proto.Msg.ActorRefData
                {
                    Path = Akka.Serialization.Serialization.SerializedActorPath(unwatch.Watchee)
                },
                Watcher = new Proto.Msg.ActorRefData
                {
                    Path = Akka.Serialization.Serialization.SerializedActorPath(unwatch.Watcher)
                }
            };
            return message.ToArray();
        }

        private static Unwatch UnwatchFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = Proto.Msg.WatchData.Parser.ParseFrom(bytes);
            return new Unwatch(
                serializer.ResolveActorRef(proto.Watchee.Path).AsInstanceOf<IInternalActorRef>(),
                serializer.ResolveActorRef(proto.Watcher.Path).AsInstanceOf<IInternalActorRef>());
        }

        //
        // Failed
        //
        private byte[] FailedToProto(Failed failed)
        {
            var message = new Proto.Msg.FailedData
            {
                Cause = _exceptionSupport.ExceptionToProto(failed.Cause),
                Child = new Proto.Msg.ActorRefData
                {
                    Path = Akka.Serialization.Serialization.SerializedActorPath(failed.Child)
                },
                Uid = (ulong)failed.Uid
            };
            return message.ToArray();
        }

        private static Failed FailedFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = Proto.Msg.FailedData.Parser.ParseFrom(bytes);

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
            var message = new Proto.Msg.DeathWatchNotificationData
            {
                Actor = new Proto.Msg.ActorRefData
                {
                    Path = Akka.Serialization.Serialization.SerializedActorPath(deathWatchNotification.Actor)
                },
                AddressTerminated = deathWatchNotification.AddressTerminated,
                ExistenceConfirmed = deathWatchNotification.ExistenceConfirmed
            };
            return message.ToArray();
        }

        private static DeathWatchNotification DeathWatchNotificationFromProto(SystemMessageSerializer serializer, byte[] bytes)
        {
            var proto = Proto.Msg.DeathWatchNotificationData.Parser.ParseFrom(bytes);

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
