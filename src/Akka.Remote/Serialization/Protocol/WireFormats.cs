using MessagePack;

namespace Akka.Remote.Serialization.Protocol
{
    #region -- Remoting message formats --

    [MessagePackObject]
    public sealed class AckAndEnvelopeContainer
    {
        [Key(0)]
        public AcknowledgementInfo Ack { get; set; }

        [Key(1)]
        public RemoteEnvelope Envelope { get; set; }
    }

    /// <summary>Defines a remote message.</summary>
    [MessagePackObject]
    public sealed class RemoteEnvelope
    {
        [Key(0)]
        public ActorRefData Recipient { get; set; }

        [Key(1)]
        public Payload Message { get; set; }

        [Key(2)]
        public ActorRefData Sender { get; set; }

        [Key(3)]
        public ulong Seq { get; set; }
    }

    [MessagePackObject]
    public sealed class AcknowledgementInfo
    {
        [Key(0)]
        public readonly ulong CumulativeAck;

        [Key(1)]
        public readonly ulong[] Nacks;

        [SerializationConstructor]
        public AcknowledgementInfo(ulong cumulativeAck, ulong[] nacks)
        {
            CumulativeAck = cumulativeAck;
            Nacks = nacks;
        }
    }

    /// <summary>Defines Akka.Remote.DaemonMsgCreate</summary>
    [MessagePackObject]
    public readonly struct DaemonMsgCreateData
    {
        [Key(0)]
        public readonly PropsData Props;
        [Key(1)]
        public readonly DeployData Deploy;
        [Key(2)]
        public readonly string Path;
        [Key(3)]
        public readonly ActorRefData Supervisor;

        [SerializationConstructor]
        public DaemonMsgCreateData(PropsData props, DeployData deploy, string path, ActorRefData supervisor)
        {
            Props = props;
            Deploy = deploy;
            Path = path;
            Supervisor = supervisor;
        }
    }

    /// <summary>Serialization of Akka.Actor.Props</summary>
    [MessagePackObject]
    public sealed class PropsData
    {
        [Key(0)]
        public readonly DeployData Deploy;

        [Key(1)]
        public readonly string Clazz;

        [Key(2)]
        public readonly Payload[] Args;

        [SerializationConstructor]
        public PropsData(DeployData deploy, string clazz, Payload[] args)
        {
            Deploy = deploy;
            Clazz = clazz;
            Args = args;
        }
    }

    /// <summary>Serialization of akka.actor.Deploy</summary>
    [MessagePackObject]
    public sealed class DeployData
    {
        [Key(0)]
        public readonly string Path;

        [Key(1)]
        public readonly Payload Config;

        [Key(2)]
        public readonly Payload RouterConfig;

        [Key(3)]
        public readonly Payload Scope;

        [Key(4)]
        public readonly string Dispatcher;

        [SerializationConstructor]
        public DeployData(string path, Payload config, Payload routerConfig, Payload scope, string dispatcher)
        {
            Path = path;
            Config = config;
            RouterConfig = routerConfig;
            Scope = scope;
            Dispatcher = dispatcher;
        }
    }

    #endregion

    #region -- Akka Protocol message formats --

    /// <summary>Message format of Akka Protocol. Message contains either a payload or an instruction.</summary>
    [MessagePackObject]
    public readonly struct AkkaProtocolMessage
    {
        [Key(0)]
        public readonly byte[] Payload;

        [Key(1)]
        public readonly AkkaControlMessage Instruction;

        [SerializationConstructor]
        public AkkaProtocolMessage(byte[] payload, AkkaControlMessage instruction)
        {
            Payload = payload;
            Instruction = instruction;
        }
    }

    /// <summary>Defines some control messages for the remoting.</summary>
    [MessagePackObject]
    public sealed class AkkaControlMessage
    {
        [Key(0)]
        public readonly CommandType CommandType;

        [Key(1)]
        public readonly AkkaHandshakeInfo HandshakeInfo;

        [SerializationConstructor]
        public AkkaControlMessage(CommandType commandType, AkkaHandshakeInfo handshakeInfo)
        {
            CommandType = commandType;
            HandshakeInfo = handshakeInfo;
        }
    }

    [MessagePackObject]
    public sealed class AkkaHandshakeInfo
    {
        [Key(0)]
        public readonly AddressData Origin;

        [Key(1)]
        public readonly ulong Uid;

        [Key(2)]
        public readonly string Cookie;

        [SerializationConstructor]
        public AkkaHandshakeInfo(AddressData origin, ulong uid, string cookie = null)
        {
            Origin = origin;
            Uid = uid;
            Cookie = cookie;
        }
    }

    /// <summary>Defines the type of the AkkaControlMessage command type</summary>
    public enum CommandType
    {
        None = 0,

        Associate = 1,

        Disassociate = 2,

        Heartbeat = 3,

        /// <summary>Remote system is going down and will not accepts new connections</summary>
        DisassociateShuttingDown = 4,

        /// <summary>Remote system refused the association since the current system is quarantined</summary>
        DisassociateQuarantined = 5,
    }

    [MessagePackObject]
    public readonly struct RemoteScope
    {
        [Key(0)]
        public readonly AddressData Node;

        [SerializationConstructor]
        public RemoteScope(AddressData node)
        {
            Node = node;
        }
    }

    #endregion

    #region -- Router configs --

    [MessagePackObject]
    public readonly struct DefaultResizer
    {
        [Key(0)]
        public readonly uint LowerBound;

        [Key(1)]
        public readonly uint UpperBound;

        [Key(2)]
        public readonly uint PressureThreshold;

        [Key(3)]
        public readonly double RampupRate;

        [Key(4)]
        public readonly double BackoffThreshold;

        [Key(5)]
        public readonly double BackoffRate;

        [Key(6)]
        public readonly uint MessagesPerResize;

        [SerializationConstructor]
        public DefaultResizer(uint lowerBound, uint upperBound, uint pressureThreshold,
            double rampupRate, double backoffThreshold, double backoffRate, uint messagesPerResize)
        {
            LowerBound = lowerBound;
            UpperBound = upperBound;
            PressureThreshold = pressureThreshold;
            RampupRate = rampupRate;
            BackoffThreshold = backoffThreshold;
            BackoffRate = backoffRate;
            MessagesPerResize = messagesPerResize;
        }
    }

    [MessagePackObject]
    public sealed class FromConfig
    {
        [Key(0)]
        public readonly Payload Resizer;

        [Key(1)]
        public readonly string RouterDispatcher;

        [SerializationConstructor]
        public FromConfig(Payload resizer, string routerDispatcher)
        {
            Resizer = resizer;
            RouterDispatcher = routerDispatcher;
        }
    }

    [MessagePackObject]
    public sealed class GenericRoutingPool
    {
        [Key(0)]
        public readonly uint NrOfInstances;

        [Key(1)]
        public readonly string RouterDispatcher;

        [Key(2)]
        public readonly bool UsePoolDispatcher;

        [Key(3)]
        public readonly Payload Resizer;

        [SerializationConstructor]
        public GenericRoutingPool(uint nrOfInstances, string routerDispatcher, bool usePoolDispatcher, Payload resizer)
        {
            NrOfInstances = nrOfInstances;
            RouterDispatcher = routerDispatcher;
            UsePoolDispatcher = usePoolDispatcher;
            Resizer = resizer;
        }
    }

    [MessagePackObject]
    public readonly struct ScatterGatherPool
    {
        [Key(0)]
        public readonly GenericRoutingPool Generic;

        [Key(1)]
        public readonly Duration Within;

        [SerializationConstructor]
        public ScatterGatherPool(GenericRoutingPool generic, Duration within)
        {
            Generic = generic;
            Within = within;
        }
    }

    [MessagePackObject]
    public readonly struct TailChoppingPool
    {
        [Key(0)]
        public readonly GenericRoutingPool Generic;

        [Key(1)]
        public readonly Duration Within;

        [Key(2)]
        public readonly Duration Interval;

        [SerializationConstructor]
        public TailChoppingPool(GenericRoutingPool generic, Duration within, Duration interval)
        {
            Generic = generic;
            Within = within;
            Interval = interval;
        }
    }

    [MessagePackObject]
    public readonly struct RemoteRouterConfig
    {
        [Key(0)]
        public readonly Payload Local;

        [Key(1)]
        public readonly AddressData[] Nodes;

        [SerializationConstructor]
        public RemoteRouterConfig(Payload local, AddressData[] nodes)
        {
            Local = local;
            Nodes = nodes;
        }
    }

    #endregion
}
