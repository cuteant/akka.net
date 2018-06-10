//-----------------------------------------------------------------------
// <copyright file="DaemonMsgCreateSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Linq;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;
using Akka.Serialization;
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
    public class DaemonMsgCreateSerializer : Serializer
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
        public override bool IncludeManifest => false;

        /// <inheritdoc />
        public override byte[] ToBinary(object obj)
        {
            if (obj is DaemonMsgCreate msg)
            {
                return MessagePackSerializer.Serialize(new Protocol.DaemonMsgCreateData(PropsToProto(msg.Props), DeployToProto(msg.Deploy), msg.Path, SerializeActorRef(msg.Supervisor)), s_defaultResolver);
            }

            throw new ArgumentException($"Can't serialize a non-DaemonMsgCreate message using DaemonMsgCreateSerializer [{obj.GetType()}]");
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, Type type)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.DaemonMsgCreateData>(bytes, s_defaultResolver);

            return new DaemonMsgCreate(
                PropsFromProto(proto.Props),
                DeployFromProto(proto.Deploy),
                proto.Path,
                DeserializeActorRef(proto.Supervisor));
        }

        //
        // Props
        //
        private Protocol.PropsData PropsToProto(Props props)
        {
            return new Protocol.PropsData(
                DeployToProto(props.Deploy),
                props.Type.TypeQualifiedName(),
                props.Arguments.Select(_ => WrappedPayloadSupport.PayloadToProto(system, _)).ToArray());

        }

        private Props PropsFromProto(Protocol.PropsData protoProps)
        {
            var actorClass = TypeUtils.ResolveType(protoProps.Clazz);
            var propsArgs = protoProps.Args;
            if (propsArgs != null)
            {
                var args = new object[propsArgs.Length];
                for (int i = 0; i < args.Length; i++)
                {
                    args[i] = WrappedPayloadSupport.PayloadFrom(system, propsArgs[i]);
                }
                return new Props(DeployFromProto(protoProps.Deploy), actorClass, args);
            }
            else
            {
                return new Props(DeployFromProto(protoProps.Deploy), actorClass, EmptyArray<object>.Instance);
            }
        }

        //
        // Deploy
        //
        private Protocol.DeployData DeployToProto(Deploy deploy)
        {
            return new Protocol.DeployData(
                deploy.Path,
                WrappedPayloadSupport.PayloadToProto(system, deploy.Config),
                deploy.RouterConfig != NoRouter.Instance ? WrappedPayloadSupport.PayloadToProto(system, deploy.RouterConfig) : null,
                deploy.Scope != Deploy.NoScopeGiven ? WrappedPayloadSupport.PayloadToProto(system, deploy.Scope) : null,
                deploy.Dispatcher != Deploy.NoDispatcherGiven ? deploy.Dispatcher : null
                );
        }

        private Deploy DeployFromProto(Protocol.DeployData protoDeploy)
        {
            var config = WrappedPayloadSupport.PayloadFrom(system, protoDeploy.Config)?.AsInstanceOf<Config>() ?? Config.Empty;

            var routerConfig = WrappedPayloadSupport.PayloadFrom(system, protoDeploy.RouterConfig)?.AsInstanceOf<RouterConfig>() ?? NoRouter.Instance;

            var scope = WrappedPayloadSupport.PayloadFrom(system, protoDeploy.Scope)?.AsInstanceOf<Scope>() ?? Deploy.NoScopeGiven;

            var dispatcher = !string.IsNullOrEmpty(protoDeploy.Dispatcher)
                ? protoDeploy.Dispatcher
                : Deploy.NoDispatcherGiven;

            return new Deploy(protoDeploy.Path, config, routerConfig, scope, dispatcher);
        }

        //
        // IActorRef
        //
        private static Protocol.ActorRefData SerializeActorRef(IActorRef actorRef)
        {
            return new Protocol.ActorRefData(Akka.Serialization.Serialization.SerializedActorPath(actorRef));
        }

        private IActorRef DeserializeActorRef(Protocol.ActorRefData actorRefData)
        {
            return system.Provider.ResolveActorRef(actorRefData.Path);
        }
    }
}
