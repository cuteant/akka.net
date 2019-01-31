using System;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Cluster.Tools.Client;
using Akka.Cluster.Tools.Client.Serialization;
using Akka.Cluster.Tools.PublishSubscribe;
using Akka.Cluster.Tools.PublishSubscribe.Serialization;
using Akka.Cluster.Tools.Singleton;
using Akka.Cluster.Tools.Singleton.Serialization;
using Akka.Configuration;
using Akka.Pattern;

namespace Akka.Cluster.Tools
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        singletonName,
        settings,
        array,
        assembly,
        buffer,
        destination,
        key,
        obj,
        s,
        str,
        source,
        type,
        types,
        value,
        values,
        valueFactory,
        name,
        item,
        options,
        list,
        ts,
        other,
        pool,
        inner,
        policy,
        offset,
        count,
        path,
        typeInfo,
        method,
        qualifiedTypeName,
        fullName,
        feature,
        manager,
        directories,
        dirEnumArgs,
        asm,
        includedAssemblies,
        func,
        defaultFn,
        returnType,
        propertyInfo,
        parameterTypes,
        fieldInfo,
        memberInfo,
        attributeType,
        pi,
        fi,
        invoker,
        instanceType,
        target,
        member,
        typeName,
        predicate,
        assemblyPredicate,
        collection,
        capacity,
        match,
        index,
        length,
        startIndex,
        args,
        typeId,
        acceptableHeartbeatPause,
        heartbeatInterval,
    }

    #endregion

    #region -- ExceptionResource --

    /// <summary>The convention for this enum is using the resource name as the enum name</summary>
    internal enum ExceptionResource
    {
    }

    #endregion

    partial class ThrowHelper
    {
        #region -- ArgumentException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_RemovalMarginMustBePositive()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("ClusterSingletonManagerSettings.RemovalMargin must be positive", "removalMargin");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_HandOverRetryIntervalMustBePositive()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("ClusterSingletonManagerSettings.HandOverRetryInterval must be positive", "handOverRetryInterval");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_SingletonIdentificationIntervalMustBePositive()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("singletonIdentificationInterval must be positive", "singletonIdentificationInterval");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_BufferSizeMustBePositive()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("bufferSize must be positive", "bufferSize");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ConsistentHashingRoutingLogicCannotBeUsedByThePubsubMediator()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("ConsistentHashingRoutingLogic cannot be used by the pub-sub mediator");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ActorSystemSettingsHasNoConfigurationForPubSubDefined()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Actor system settings has no configuration for akka.cluster.pub-sub defined");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ConsistentHashingRoutingLogicCannotBeUsedByThePubSubMediator()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Consistent hashing routing logic cannot be used by the pub-sub mediator");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_TopicMustBeDefined()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("topic must be defined");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_InitialContactsMustBeDefined()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("InitialContacts must be defined");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_BufferSizeMustBe0_10000()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("BufferSize must be >= 0 and <= 10000");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_InitialContactsForClusterClientCannotBeEmpty()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Initial contacts for cluster client cannot be empty");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ActorSystemDoesnotHave_AkkaClusterClientReceptionist_Config(ActorSystem system)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Actor system [{system.Name}] doesn't have `akka.cluster.client.receptionist` config set up");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ActorSystemDoesnotHave_AkkaClusterClient_Config(ActorSystem system)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Actor system [{system.Name}] doesn't have `akka.cluster.client` config set up");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_UnknownRoutingLogicIsTriedToBeAppliedToThePubSubMediator(string routingLogicName)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Unknown routing logic is tried to be applied to the pub-sub mediator: " + routingLogicName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ThisCusterMemberDoesNotHaveTheRole(Cluster cluster, string role)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"This cluster member [{cluster.SelfAddress}] doesn't have the role [{role}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ThisCusterMemberDoesNotHaveTheRole(Cluster cluster, DistributedPubSubSettings settings)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"The cluster member [{cluster.SelfAddress}] doesn't have the role [{settings.Role}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ThisCusterMemberDoesNotHaveARole(Cluster cluster, ClusterReceptionistSettings settings)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"This cluster member [{cluster.SelfAddress}] does not have a role [{settings.Role}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static byte[] ThrowArgumentException_Serializer_ClusterClientMessage(object obj)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Can't serialize object of type [{obj.GetType()}] in [{nameof(ClusterClientMessageSerializer)}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static object ThrowArgumentException_Serializer_ClusterClientMessage(int manifest)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in serializer {nameof(ClusterClientMessageSerializer)}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static byte[] ThrowArgumentException_Serializer_DistributedPubSubMessage(object obj)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Can't serialize object of type {obj.GetType()} with {nameof(DistributedPubSubMessageSerializer)}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static object ThrowArgumentException_Serializer_DistributedPubSubMessage(int manifest)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in serializer {nameof(DistributedPubSubMessageSerializer)}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static object ThrowArgumentException_Serializer_ClusterSingletonMessage(int manifest)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in [{nameof(ClusterSingletonMessageSerializer)}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T ThrowArgumentException_Serializer_ClusterSingletonMessage<T>(object o)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Cannot serialize object of type [{o.GetType()}] in [{nameof(ClusterSingletonMessageSerializer)}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T ThrowArgumentException_Manifest_ClusterClientMessage<T>(object o)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Can't serialize object of type [{(o as Type) ?? o.GetType()}] in [{nameof(ClusterClientMessageSerializer)}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T ThrowArgumentException_Manifest_DistributedPubSubMessage<T>(object o)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Serializer {nameof(DistributedPubSubMessageSerializer)} cannot serialize message of type {o.GetType()}");
            }
        }

        #endregion

        #region -- IllegalStateException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_ClusterNodeMustNotBeTerminated()
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException("Cluster node must not be terminated");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int ThrowIllegalStateException_UnexpectedAddressWithoutHostPort(Address node)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException("Unexpected address without host/port: " + node);
            }
        }

        #endregion

        #region -- ConfigurationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_CannotCreateClusterSingletonProxySettings()
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Cannot create {typeof(ClusterSingletonProxySettings)}: akka.cluster.singleton-proxy configuration node not found");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_CannotInitializeClusterSingletonManagerSettings()
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException(
                    $"Cannot initialize {typeof(ClusterSingletonManagerSettings)}: akka.cluster.singleton configuration node was not provided");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_MinNumberOfHandOverRetriesMustBe_1()
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException("min-number-of-hand-over-retries must be >= 1");
            }
        }

        #endregion

        #region -- ClusterSingletonManagerIsStuckException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowClusterSingletonManagerIsStuckException_BecomingSingletonOldest(BecomingOldestData becomingOldest)
        {
            throw GetException();
            ClusterSingletonManagerIsStuckException GetException()
            {
                return new ClusterSingletonManagerIsStuckException($"Becoming singleton oldest was stuck because previous oldest [{becomingOldest.PreviousOldest}] is unresponsive");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowClusterSingletonManagerIsStuckException_ExpectedHandOverTo(WasOldestData wasOldestData)
        {
            throw GetException();
            ClusterSingletonManagerIsStuckException GetException()
            {
                return new ClusterSingletonManagerIsStuckException($"Expected hand-over to [{wasOldestData.NewOldest}] never occurred");
            }
        }

        #endregion
    }
}
