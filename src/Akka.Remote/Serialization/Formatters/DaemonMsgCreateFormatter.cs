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
using MessagePack.Formatters;

namespace Akka.Remote.Serialization.Formatters
{
    internal sealed class DaemonMsgCreateFormatter : IMessagePackFormatter<DaemonMsgCreate>
    {
        private static readonly IFormatterResolver DefaultResolver = MessagePackSerializer.DefaultResolver;
        public static readonly IMessagePackFormatter<DaemonMsgCreate> Instance = new DaemonMsgCreateFormatter();

        public DaemonMsgCreate Deserialize(ref MessagePackReader reader, IFormatterResolver formatterResolver)
        {
            if (reader.IsNil()) { return default; }

            var formatter = formatterResolver.GetFormatterWithVerify<DaemonMsgCreateData>();
            var proto = formatter.Deserialize(ref reader, DefaultResolver);

            var system = formatterResolver.GetActorSystem();
            return new DaemonMsgCreate(
                PropsFromProto(system, proto.Props),
                DeployFromProto(system, proto.Deploy),
                proto.Path,
                DeserializeActorRef(system, proto.Supervisor));
        }

        public void Serialize(ref MessagePackWriter writer, ref int idx, DaemonMsgCreate value, IFormatterResolver formatterResolver)
        {
            if (value == null) { writer.WriteNil(ref idx); return; }

            var system = formatterResolver.GetActorSystem();
            var protoMessage = new DaemonMsgCreateData(
                    PropsToProto(system, value.Props),
                    DeployToProto(system, value.Deploy),
                    value.Path,
                    SerializeActorRef(value.Supervisor));

            var formatter = formatterResolver.GetFormatterWithVerify<DaemonMsgCreateData>();
            formatter.Serialize(ref writer, ref idx, protoMessage, DefaultResolver);
        }

        //
        // Props
        //
        private static PropsData PropsToProto(ExtendedActorSystem system, Props props)
        {
            return new PropsData(
                DeployToProto(system, props.Deploy),
                props.Type.TypeQualifiedName(),
                props.Arguments.Select(_ => system.SerializeMessage(_)).ToArray());

        }

        private static Props PropsFromProto(ExtendedActorSystem system, in PropsData protoProps)
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
        private static DeployData DeployToProto(ExtendedActorSystem system, Deploy deploy)
        {
            return new DeployData(
                deploy.Path,
                system.SerializeMessage(deploy.Config),
                deploy.RouterConfig != NoRouter.Instance ? system.SerializeMessage(deploy.RouterConfig) : Payload.Null,
                deploy.Scope != Deploy.NoScopeGiven ? system.SerializeMessage(deploy.Scope) : Payload.Null,
                deploy.Dispatcher != Deploy.NoDispatcherGiven ? deploy.Dispatcher : null
                );
        }

        private static Deploy DeployFromProto(ExtendedActorSystem system, in DeployData protoDeploy)
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
        private static ReadOnlyActorRefData SerializeActorRef(IActorRef actorRef)
        {
            return new ReadOnlyActorRefData(Akka.Serialization.Serialization.SerializedActorPath(actorRef));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static IActorRef DeserializeActorRef(ExtendedActorSystem system, ReadOnlyActorRefData actorRefData)
        {
            return system.Provider.ResolveActorRef(actorRefData.Path);
        }
    }
}
