//-----------------------------------------------------------------------
// <copyright file="DaemonMsgCreateSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;
using Akka.Serialization;
using Akka.Serialization.Protocol;
using Akka.Util;
using Akka.Util.Internal;
using CuteAnt;
using CuteAnt.Reflection;
using MessagePack;

namespace Akka.Remote.Serialization
{
    /// <summary>
    /// Serializes Akka's internal <see cref="DaemonMsgCreate"/> using protobuf.
    /// </summary>
    public sealed class DaemonMsgCreateSerializer : Serializer
    {
        private static readonly IFormatterResolver s_defaultResolver = MessagePackSerializer.DefaultResolver;

        /// <summary>
        /// Initializes a new instance of the <see cref="DaemonMsgCreateSerializer"/> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer. </param>
        public DaemonMsgCreateSerializer(ExtendedActorSystem system) : base(system)
        {
        }

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            var msg = obj as DaemonMsgCreate;
            if (null == msg) { ThrowHelper.ThrowArgumentException_Serializer_DaemonMsg(obj); }

            return MessagePackSerializer.Serialize(
                new DaemonMsgCreateData(PropsToProto(system, msg.Props),
                    DeployToProto(system, msg.Deploy),
                    msg.Path,
                    SerializeActorRef(msg.Supervisor)),
                s_defaultResolver);
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, Type type)
        {
            var proto = MessagePackSerializer.Deserialize<DaemonMsgCreateData>(bytes, s_defaultResolver);

            return new DaemonMsgCreate(
                PropsFromProto(system, proto.Props),
                DeployFromProto(system, proto.Deploy),
                proto.Path,
                DeserializeActorRef(system, proto.Supervisor));
        }

        //
        // Props
        //
        internal static PropsData PropsToProto(ExtendedActorSystem system, Props props)
        {
            return new PropsData(
                DeployToProto(system, props.Deploy),
                props.Type.TypeQualifiedName(),
                props.Arguments.Select(_ => system.Serialize(_)).ToArray());

        }

        internal static Props PropsFromProto(ExtendedActorSystem system, in PropsData protoProps)
        {
            var actorClass = TypeUtils.ResolveType(protoProps.Clazz);
            var propsArgs = protoProps.Args;
            if (propsArgs != null)
            {
                var args = new object[propsArgs.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = system.Deserialize(propsArgs[i]);
                }
                return new Props(DeployFromProto(system, protoProps.Deploy), actorClass, args);
            }
            else
            {
                return new Props(DeployFromProto(system, protoProps.Deploy), actorClass, EmptyArray<object>.Instance);
            }
        }

        //
        // Deploy
        //
        internal static DeployData DeployToProto(ExtendedActorSystem system, Deploy deploy)
        {
            return new DeployData(
                deploy.Path,
                system.Serialize(deploy.Config),
                deploy.RouterConfig != NoRouter.Instance ? system.Serialize(deploy.RouterConfig) : Payload.Null,
                deploy.Scope != Deploy.NoScopeGiven ? system.Serialize(deploy.Scope) : Payload.Null,
                deploy.Dispatcher != Deploy.NoDispatcherGiven ? deploy.Dispatcher : null
                );
        }

        internal static Deploy DeployFromProto(ExtendedActorSystem system, in DeployData protoDeploy)
        {
            var config = system.Deserialize(protoDeploy.Config)?.AsInstanceOf<Config>() ?? Config.Empty;

            var routerConfig = system.Deserialize(protoDeploy.RouterConfig)?.AsInstanceOf<RouterConfig>() ?? NoRouter.Instance;

            var scope = system.Deserialize(protoDeploy.Scope)?.AsInstanceOf<Scope>() ?? Deploy.NoScopeGiven;

            var dispatcher = !string.IsNullOrEmpty(protoDeploy.Dispatcher)
                ? protoDeploy.Dispatcher
                : Deploy.NoDispatcherGiven;

            return new Deploy(protoDeploy.Path, config, routerConfig, scope, dispatcher);
        }

        //
        // IActorRef
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static ReadOnlyActorRefData SerializeActorRef(IActorRef actorRef)
        {
            return new ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(actorRef));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static IActorRef DeserializeActorRef(ExtendedActorSystem system, ReadOnlyActorRefData actorRefData)
        {
            return system.Provider.ResolveActorRef(actorRefData.Path);
        }
    }
}
