using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Actor.Internal;
using Akka.Configuration;
using Akka.Dispatch;
using Akka.Dispatch.SysMsg;
using Akka.Pattern;
using Akka.Routing;
using Akka.Tools.MatchHandler;
using Akka.Util;

namespace Akka
{
    partial class AkkaThrowHelper
    {
        #region -- Exception --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_IPAddress_Num()
        {
            throw GetException();

            static Exception GetException()
            {
                return new Exception("IPAddress.m_Numbers not found");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_IPAddress_OnlyIpV6()
        {
            throw GetException();

            static Exception GetException()
            {
                return new Exception("Only AddressFamily.InterNetworkV6 can be converted to IPv4");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_IPAddress_OnlyIpV4()
        {
            throw GetException();

            static Exception GetException()
            {
                return new Exception("Only AddressFamily.InterNetworkV4 can be converted to IPv6");
            }
        }

        #endregion

        #region -- ArgumentException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_ExpectedConsistentHashingPool(RouterConfig routerConfig)
        {
            return new ArgumentException($"Expected ConsistentHashingPool, got {routerConfig}", nameof(routerConfig));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_ExpectedConsistentHashingGroup(RouterConfig routerConfig)
        {
            return new ArgumentException($"Expected ConsistentHashingGroup, got {routerConfig}", nameof(routerConfig));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_TokenBucket_Capacity()
        {
            throw GetException();

            static ArgumentException GetException()
            {
                return new ArgumentException("Capacity must be non-negative", "capacity");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_TokenBucket_Time()
        {
            throw GetException();

            static ArgumentException GetException()
            {
                return new ArgumentException("Time between tokens must be larger than zero ticks.", "ticksBetweenTokens");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_TokenBucket_Offer()
        {
            throw GetException();

            static ArgumentException GetException()
            {
                return new ArgumentException("Cost must be non-negative", "cost");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_NotEnoughBitsToMakeAByte()
        {
            throw GetException();

            static ArgumentException GetException()
            {
                return new ArgumentException("Not enough bits to make a byte!", "arr");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Config_can_not_have_itself_as_fallback()
        {
            throw GetException();

            static ArgumentException GetException()
            {
                return new ArgumentException("Config can not have itself as fallback", "fallback");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_LogLevel(string logLevel)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($@"Unknown LogLevel: ""{logLevel}"". Valid values are: ""Debug"", ""Info"", ""Warning"", ""Error""", nameof(logLevel));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IStash ThrowArgumentException_StashFactoryCreate(Type actorType)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Actor {actorType} implements an unrecognized subclass of {typeof(IActorStash)} - cannot instantiate", nameof(actorType));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ActorCellMakeChild(IInternalActorRef self, string name)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Pre-creation serialization check failed at [${self.Path}/{name}]", nameof(name));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_PartialActionBuilder(int MaxNumberOfArguments)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Too many arguments. Max {MaxNumberOfArguments} arguments allowed.", "handlerAndArgs");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_AddrCompareTo(object obj)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Cannot compare {nameof(Address)} with instance of type '{obj?.GetType().FullName ?? "null"}'.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_CoordinatedShutdownTimeout(string phase)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unknown phase [{phase}]. All phases must be defined in configuration.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_CoordinatedShutdownSort(string u, Dictionary<string, Phase> phases)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Cycle detected in graph of phases. It must be a DAG. " + $"phase [{u}] depepends transitively on itself. All dependencies: {phases}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IIndirectActorProducer ThrowArgumentException_PropsCreateProducer(Type type)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unknown actor producer [{type.FullName}]", nameof(type));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Mailboxes_Instantiate(Type type, string id, Exception ex)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Cannot instantiate MailboxType {type}, defined in [{id}]. Make sure it has a public " +
                                             "constructor with [Akka.Actor.Settings, Akka.Configuration.Config] parameters", ex);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static Type ThrowArgumentException_Mailboxes_ProducedMessageQueueType(MailboxType mailboxType)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException(nameof(mailboxType), $"No IProducesMessageQueue<TQueue> supplied for {mailboxType}; illegal mailbox type definition.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Mailboxes_VerifyRequirements_Dispatcher(Lazy<Type> mqType, string id, Type mailboxRequirement)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"produced message queue type [{mqType.Value}] does not fulfill requirement for dispatcher [{id}]." + $"Must be a subclass of [{mailboxRequirement}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Mailboxes_VerifyRequirements_Actor(Lazy<Type> mqType, Type actorType, Lazy<Type> actorRequirement)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"produced message queue type of [{mqType.Value}] does not fulfill requirement for actor class [{actorType}]." + $"Must be a subclass of [{actorRequirement.Value}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_DefaultResizer_Lower(int lower)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"lowerBound must be >= 0, was: {lower}", nameof(lower));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_DefaultResizer_Upper(int upper)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"upperBound must be >= 0, was: {upper}", nameof(upper));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_DefaultResizer_UpperLessthanLower(int lower, int upper)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"upperBound must be >= lowerBound, was: {upper} < {lower}", nameof(upper));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_DefaultResizer_RampupRate(double rampupRate)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"rampupRate must be >= 0.0, was {rampupRate}", nameof(rampupRate));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_DefaultResizer_BackoffThreshold(double backoffThreshold)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"backoffThreshold must be <= 1.0, was {backoffThreshold}", nameof(backoffThreshold));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_DefaultResizer_BackoffRate(double backoffRate)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"backoffRate must be >= 0.0, was {backoffRate}", nameof(backoffRate));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_DefaultResizer_MessagesPerResize(int messagesPerResize)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"messagesPerResize must be > 0, was {messagesPerResize}", nameof(messagesPerResize));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Serializer_ActorSel(object obj)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Cannot serialize object of type [{obj?.GetType().TypeQualifiedName()}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_Object_must_be_of_type_IActorRef(object obj)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Object must be of type IActorRef, found {obj.GetType()} instead.", nameof(obj));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_Serializer_D(object obj)
        {
            var type = obj as Type;
            var typeQualifiedName = type is object ? type.FullName : obj?.GetType().FullName;
            return new ArgumentException($"Cannot deserialize object of type [{typeQualifiedName}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_Serializer_SystemMsg_NoMessage()
        {
            return new ArgumentException("NoMessage should never be serialized or deserialized");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_Serializer_S(object obj)
        {
            return new ArgumentException($"Cannot serialize object of type [{obj?.GetType().TypeQualifiedName()}]");
        }

        #endregion

        #region -- ArgumentNullException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_ProducerCannotBeNull()
        {
            throw GetException();

            static ArgumentNullException GetException()
            {
                return new ArgumentNullException("producer", "Producer cannot be null");
            }
        }

        #endregion

        #region -- ArgumentOutOfRangeException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentOutOfRangeException GetArgumentOutOfRangeException()
        {
            return new ArgumentOutOfRangeException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_Cancelable_Delay(TimeSpan delay)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(delay), $"The delay must be >0, it was {delay}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_Cancelable_MillDelay(int millisecondsDelay)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(millisecondsDelay), $"The delay must be >0, it was {millisecondsDelay}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_HashedWheelTimerScheduler_Min(int ticksPerWheel)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(ticksPerWheel), ticksPerWheel, "Must be greater than 0.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_HashedWheelTimerScheduler_Max(int ticksPerWheel)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(nameof(ticksPerWheel), ticksPerWheel, "Cannot be greater than 2^30.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_ValidateInterval(TimeSpan interval)
        {
            throw GetArgumentOutOfRangeException();
            ArgumentOutOfRangeException GetArgumentOutOfRangeException()
            {
                return new ArgumentOutOfRangeException(nameof(interval), $"Interval must be >0. It was {interval}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_ValidateDelay(TimeSpan delay)
        {
            throw GetArgumentOutOfRangeException();
            ArgumentOutOfRangeException GetArgumentOutOfRangeException()
            {
                return new ArgumentOutOfRangeException(nameof(delay), $"Delay must be >=0. It was {delay}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_ValidateDelay(TimeSpan delay, AkkaExceptionArgument argument)
        {
            throw GetArgumentOutOfRangeException();
            ArgumentOutOfRangeException GetArgumentOutOfRangeException()
            {
                return new ArgumentOutOfRangeException(GetArgumentName(argument), $"Delay must be >=0. It was {delay}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_MatchExpressionBuilder_Add(HandlerKind kind)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException(
                    $"This should not happen. The value {typeof(HandlerKind)}.{kind} is a new enum value that has been added without updating the code in this method.");
            }
        }

        #endregion

        #region -- ConfigurationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ConfigurationException GetConfigurationException_CouldNotResolveSupervisorstrategyconfigurator()
        {
            return new ConfigurationException("Could not resolve SupervisorStrategyConfigurator. typeName is null");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ConfigurationException GetConfigurationException_ProblemWhileCreating(ActorPath path, Props props, Exception ex)
        {
            return new ConfigurationException(
                $"Configuration problem while creating [{path}] with dispatcher [{props.Dispatcher}] and mailbox [{props.Mailbox}]", ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ConfigurationException GetConfigurationException_ProblemWhileCreating1(ActorPath path, Props routerProps, Props routeeProps, Exception ex)
        {
            return new ConfigurationException(
                $"Configuration problem while creating [{path}] with router dispatcher [{routerProps.Dispatcher}] and mailbox [{routerProps.Mailbox}] and routee dispatcher [{routeeProps.Dispatcher}] and mailbox [{routeeProps.Mailbox}].", ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ConfigurationException GetConfigurationException_DispatcherNotConfiguredForRouteesOfPath(Props p, ActorPath path)
        {
            return new ConfigurationException($"Dispatcher [{p.Dispatcher}] not configured for routees of path [{path}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ConfigurationException GetConfigurationException_DispatcherNotConfiguredForRouterOfPath(Props p, ActorPath path)
        {
            return new ConfigurationException($"Dispatcher [{p.RouterConfig.RouterDispatcher}] not configured for router of path [{path}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_CouldNotResolveSupervisorstrategyconfiguratorType(string typeName)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Could not resolve SupervisorStrategyConfigurator type {typeName}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_CouldNotResolveExecutorServiceConfiguratorType(string executor, Config config)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Could not resolve executor service configurator type {executor} for path {config.GetString("id", "unknown")}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_Invalid_phase_configuration()
        {
            throw GetException();
            static ConfigurationException GetException()
            {
                return new ConfigurationException("Invalid phase configuration.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_Akka_Coordinated_Shutdown_Config_Cannot_Be_Empty()
        {
            throw GetException();
            static ConfigurationException GetException()
            {
                return new ConfigurationException("akka.coordinated-shutdown config cannot be empty");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_Dispatcher_None(string id)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Dispatcher {id} not configured.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_Dispatcher_TypeIsNull(string id)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Could not resolve dispatcher for path {id}. type is null");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_Dispatcher_InvalidType(string type, string id)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Could not resolve dispatcher type {type} for path {id}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_Dispatcher_Id(Config cfg)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Missing dispatcher `id` property in config: {cfg.Root}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_Mailboxes_None(string id)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Mailbox Type [{id}] not configured");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_Mailboxes_TypeName(string id)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"The setting mailbox-type defined in [{id}] is empty");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_Mailboxes_Type(string mailboxTypeName, string id)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Found mailbox-type [{mailboxTypeName}] in configuration for [{id}], but could not find that type in any loaded assemblies.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_Mailboxes_Lookup(Type queueType)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Mailbox Mapping for [{queueType}] not configured");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_UnknownPhase(string phase, HashSet<string> knownPhases)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Unknown phase [{phase}], known phases [{string.Join(",", knownPhases)}]. " +
                    "All phases (along with their optional dependencies) must be defined in configuration.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_DispatcherNotConfiguredForPath(Props props2, ActorPath path)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Dispatcher [{props2.Dispatcher}] not configured for path {path}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_Cannot_retrieve_mailbox_type_from_config(string path)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Cannot retrieve mailbox type from config: {path} configuration node not found");
            }
        }

        #endregion

        #region -- InvalidOperationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_Current_node_should_not_be_null()
        {
            throw GetException();

            static InvalidOperationException GetException()
            {
                return new InvalidOperationException("Current node should not be null");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_ConsistentHash_IsEmpty(byte[] key)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"Can't get node for [{key}] from an empty node ring");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_ConsistentHash_IsEmpty(string key)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"Can't get node for [{key}] from an empty node ring");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IChildrenContainer ThrowInvalidOperationException_TerminatedChildrenContainer_Reserve(string name)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"Cannot reserve actor name '{name}': already terminated");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_TerminatingChildrenContainer_Reserve(string name)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($@"Cannot reserve actor name ""{name}"". It is terminating.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_HashedWheelTimerScheduler_Start(int workerState)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"Worker in invalid state: {workerState}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_Execute_Deadline(long currentDeadline, long deadline)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"SchedulerRegistration.Deadline [{currentDeadline}] > Timer.Deadline [{deadline}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_Dispatcher_Reg(ActorCell owner)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"Cannot register to anyone but {owner}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_Payload_Format()
        {
            throw GetException();

            static InvalidOperationException GetException()
            {
                return new InvalidOperationException("Invalid Payload format");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_CurrentTransportInformation_is_not_set()
        {
            throw GetException();

            static InvalidOperationException GetException()
            {
                return new InvalidOperationException("CurrentTransportInformation is not set. Use Serialization.WithTransport<T>.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_Too_early_access_of_SerializationInformation()
        {
            throw GetException();

            static InvalidOperationException GetException()
            {
                return new InvalidOperationException("Too early access of SerializationInformation");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_Trying_to_remove_FunctionRef_from_wrong_ActorCell(FunctionRef functionRef)
        {
            throw GetException();

            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"Trying to remove FunctionRef {functionRef.Path} from wrong ActorCell");
            }
        }

        #endregion

        #region -- NotSupportedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException()
        {
            throw GetNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static NotSupportedException GetNotSupportedException()
        {
            return new NotSupportedException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException(AkkaExceptionResource resource)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException(GetResourceString(resource));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException(ActorCell actorCell)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                string message = $@"DequeBasedMailbox required, got: {actorCell.Mailbox.GetType().Name}
An (unbounded) deque-based mailbox can be configured as follows:
    my-custom-mailbox {{
        mailbox-type = ""Akka.Dispatch.UnboundedDequeBasedMailbox""
    }}";
                return new NotSupportedException(message);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException_ActorCell_SysMsgInvokeAll(SystemMessage m)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException($"Unknown message {m.GetType().Name}");
            }
        }

        #endregion

        #region -- UriFormatException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowUriFormatException_ActorPath(string path)
        {
            throw GetException();
            UriFormatException GetException()
            {
                return new UriFormatException($"Can not parse an ActorPath: {path}");
            }
        }

        #endregion

        #region -- TimeoutException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTimeoutException_InboxDidntReceiveResponseMsgInSpecifiedTimeout(IActorRef receiver, TimeSpan timeout)
        {
            throw GetException();
            TimeoutException GetException()
            {
                return new TimeoutException(
                    $"Inbox {receiver.Path} didn't receive a response message in specified timeout {timeout}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTimeoutException_InboxReceivedAStatusFailureResponseMsg(IActorRef receiver, Status.Failure received)
        {
            throw GetException();
            TimeoutException GetException()
            {
                return new TimeoutException(
                    $"Inbox {receiver.Path} received a status failure response message: {received.Cause.Message}", received.Cause);
            }
        }

        #endregion

        #region -- TypeLoadException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static TypeLoadException GetTypeLoadException_ErrorWhileCreatingActorInstanceOfType(Type type, object[] arguments, Exception e)
        {
            return new TypeLoadException(
                $"Error while creating actor instance of type {type} with {arguments.Length} args: ({StringFormat.SafeJoin(",", arguments)})", e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTypeLoadException_UnableToFindATypeNamed(string qualifiedTypeName)
        {
            throw GetTypeLoadException();
            TypeLoadException GetTypeLoadException()
            {
                return new TypeLoadException($"Unable to find a type named {qualifiedTypeName}");
            }
        }

        #endregion

        #region -- Akka Execptions --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowRejectedExecutionException()
        {
            throw GetException();

            static RejectedExecutionException GetException()
            {
                return new RejectedExecutionException("ForkJoinExecutor is shutting down");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalActorNameException(Deploy d)
        {
            throw GetException();
            IllegalActorNameException GetException()
            {
                return new IllegalActorNameException($"Actor name in deployment [{d.Path}] must not be empty");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalActorNameException(string t, Deploy d)
        {
            throw GetException();
            IllegalActorNameException GetException()
            {
                return new IllegalActorNameException(
                    $"Illegal actor name [{t}] in deployment [${d.Path}]. Actor paths MUST: not start with `$`, include only ASCII letters and can only contain these special characters: ${new string(ActorPath.ValidSymbols)}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidMessageException(AkkaExceptionResource resource)
        {
            throw GetException();
            InvalidMessageException GetException()
            {
                return new InvalidMessageException(GetResourceString(resource));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowActorNotFoundException(AkkaExceptionResource resource)
        {
            throw GetException();
            ActorNotFoundException GetException()
            {
                return new ActorNotFoundException(GetResourceString(resource));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowActorNotFoundException(AkkaExceptionResource resource, Exception ex)
        {
            throw GetActorNotFoundException(resource, ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ActorNotFoundException GetActorNotFoundException(AkkaExceptionResource resource, Exception ex)
        {
            return new ActorNotFoundException(GetResourceString(resource), ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowDeathPactException(Terminated terminatedMessage)
        {
            throw GetException();
            DeathPactException GetException()
            {
                return new DeathPactException(terminatedMessage.ActorRef);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowActorInitializationException(AkkaExceptionResource resource)
        {
            throw GetException();
            ActorInitializationException GetException()
            {
                return new ActorInitializationException(GetResourceString(resource));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowActorInitializationException_ActorCell_CreateEx(IActorRef actorRef, Exception e)
        {
            throw GetException();
            ActorInitializationException GetException()
            {
                return new ActorInitializationException(actorRef, "Exception during creation", e);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowActorInitializationException_ResizablePoolActor(IUntypedActorContext context)
        {
            throw GetException();
            ActorInitializationException GetException()
            {
                return new ActorInitializationException($"Resizable router actor can only be used when resizer is defined, not in {context.GetType()}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowActorInitializationException_RouterActor(IUntypedActorContext context)
        {
            throw GetException();
            ActorInitializationException GetException()
            {
                return new ActorInitializationException($"Router actor can only be used in RoutedActorRef, not in {context.GetType()}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowActorInitializationException_RouterPoolActor(RouterConfig routerConfig)
        {
            throw GetException();
            ActorInitializationException GetException()
            {
                return new ActorInitializationException($"RouterPoolActor can only be used with Pool, not {routerConfig.GetType()}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException(AkkaExceptionResource resource)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException(GetResourceString(resource));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidActorNameException(AkkaExceptionResource resource)
        {
            throw GetException();
            InvalidActorNameException GetException()
            {
                return new InvalidActorNameException(GetResourceString(resource));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalActorStateException_Stash(object currMsg)
        {
            throw GetException();
            IllegalActorStateException GetException()
            {
                return new IllegalActorStateException($"Can't stash the same message {currMsg} more than once");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowStashOverflowException_Stash(object currMsg, ActorCell actorCell)
        {
            throw GetException();
            StashOverflowException GetException()
            {
                return new StashOverflowException($"Couldn't enqueue message {currMsg} to stash of {actorCell.Self}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidActorNameException_NeedUnique(string name)
        {
            throw GetException();
            InvalidActorNameException GetException()
            {
                return new InvalidActorNameException($@"Actor name ""{name}"" is not unique!");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidActorNameException_Path(string name)
        {
            throw GetException();
            InvalidActorNameException GetException()
            {
                return new InvalidActorNameException($"Illegal actor name [{name}]. Actor paths MUST: not start with `$`, include only ASCII letters and can only contain these special characters: ${new string(ActorPath.ValidSymbols)}.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSchedulerException()
        {
            throw GetException();

            static SchedulerException GetException()
            {
                return new SchedulerException("cannot enqueue after timer shutdown");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowFormatException_ExpectedPositiveValue(double value)
        {
            throw GetException();
            FormatException GetException()
            {
                return new FormatException($"Expected a positive value instead of {value}");
            }
        }

        #endregion
    }
}
