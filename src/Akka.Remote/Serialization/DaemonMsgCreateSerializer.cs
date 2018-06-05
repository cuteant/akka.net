//-----------------------------------------------------------------------
// <copyright file="DaemonMsgCreateSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using Akka.Actor;
using Akka.Configuration;
using Akka.Routing;
using Akka.Serialization;
using Akka.Util;
using Akka.Util.Internal;
using CuteAnt.Reflection;
using Google.Protobuf;

namespace Akka.Remote.Serialization
{
    /// <summary>
    /// Serializes Akka's internal <see cref="DaemonMsgCreate"/> using protobuf.
    /// </summary>
    public class DaemonMsgCreateSerializer : Serializer
    {
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
                var message = new Proto.Msg.DaemonMsgCreateData
                {
                    Props = PropsToProto(msg.Props),
                    Deploy = DeployToProto(msg.Deploy),
                    Path = msg.Path,
                    Supervisor = SerializeActorRef(msg.Supervisor)
                };

                return message.ToArray();
            }

            throw new ArgumentException($"Can't serialize a non-DaemonMsgCreate message using DaemonMsgCreateSerializer [{obj.GetType()}]");
        }

        /// <inheritdoc />
        public override object FromBinary(byte[] bytes, Type type)
        {
            var proto = Proto.Msg.DaemonMsgCreateData.Parser.ParseFrom(bytes);

            return new DaemonMsgCreate(
                PropsFromProto(proto.Props),
                DeployFromProto(proto.Deploy),
                proto.Path,
                DeserializeActorRef(proto.Supervisor));
        }

        //
        // Props
        //
        private Proto.Msg.PropsData PropsToProto(Props props)
        {
            var propsBuilder = new Proto.Msg.PropsData
            {
                Clazz = props.Type.TypeQualifiedName(),
                Deploy = DeployToProto(props.Deploy)
            };
            foreach (object arg in props.Arguments)
            {
                propsBuilder.Args.Add(WrappedPayloadSupport.PayloadToProto(system, arg));
            }

            return propsBuilder;
        }

        private Props PropsFromProto(Proto.Msg.PropsData protoProps)
        {
            var actorClass = TypeUtils.ResolveType(protoProps.Clazz);
            var args = new object[protoProps.Args.Count];
            for (int i = 0; i < args.Length; i++)
            {
                args[i] = WrappedPayloadSupport.PayloadFrom(system, protoProps.Args[i]);
            }

            return new Props(DeployFromProto(protoProps.Deploy), actorClass, args);
        }

        //
        // Deploy
        //
        private Proto.Msg.DeployData DeployToProto(Deploy deploy)
        {
            var deployBuilder = new Proto.Msg.DeployData
            {
                Path = deploy.Path
            };

            deployBuilder.Config = WrappedPayloadSupport.PayloadToProto(system, deploy.Config);

            deployBuilder.RouterConfig = deploy.RouterConfig != NoRouter.Instance ? WrappedPayloadSupport.PayloadToProto(system, deploy.RouterConfig) : WrappedPayloadSupport.Empty;

            deployBuilder.Scope = deploy.Scope != Deploy.NoScopeGiven ? WrappedPayloadSupport.PayloadToProto(system, deploy.Scope) : WrappedPayloadSupport.Empty;

            if (deploy.Dispatcher != Deploy.NoDispatcherGiven)
            {
                deployBuilder.Dispatcher = deploy.Dispatcher;
            }

            return deployBuilder;
        }

        private Deploy DeployFromProto(Proto.Msg.DeployData protoDeploy)
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
        private static Proto.Msg.ActorRefData SerializeActorRef(IActorRef actorRef)
        {
            return new Proto.Msg.ActorRefData
            {
                Path = Akka.Serialization.Serialization.SerializedActorPath(actorRef)
            };
        }

        private IActorRef DeserializeActorRef(Proto.Msg.ActorRefData actorRefData)
        {
            return system.Provider.ResolveActorRef(actorRefData.Path);
        }
    }
}
