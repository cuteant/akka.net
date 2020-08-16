using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Cluster.Sharding.Serialization;
using Akka.Pattern;

namespace Akka.Cluster.Sharding
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
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
        internal static void ThrowArgumentException_shuttingDownRegions_must_be_a_subset_of_regions()
        {
            throw GetException();

            static ArgumentException GetException()
            {
                return new ArgumentException($"'shuttingDownRegions' must be a subset of 'regions'.", "shuttingDownRegions");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ShardTypeMustBeStartedFirst(string typeName)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Shard type [{typeName}] must be started first");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_RegionIsAlreadyRegistered(PersistentShardCoordinator.ShardRegionRegistered message)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Region {message.Region} is already registered", "e");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_RegionProxyIsAlreadyRegistered(PersistentShardCoordinator.ShardRegionProxyRegistered message)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Region proxy {message.RegionProxy} is already registered", "e");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_TerminatedRegionNotRegistered(PersistentShardCoordinator.ShardRegionTerminated message)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Terminated region {message.Region} not registered", "e");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_TerminatedRegionProxyNotRegistered(PersistentShardCoordinator.ShardRegionProxyTerminated message)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Terminated region proxy {message.RegionProxy} not registered", "e");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_RegionNotRegistered(PersistentShardCoordinator.ShardHomeAllocated message)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Region {message.Region} not registered", "e");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ShardIsAlreadyAllocated(PersistentShardCoordinator.ShardHomeAllocated message)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Shard {message.Shard} is already allocated", "e");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ShardNotAllocated(PersistentShardCoordinator.ShardHomeDeallocated message)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Shard {message.Shard} not allocated", "e");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_RegionForShardNotRegistered(IActorRef region, PersistentShardCoordinator.ShardHomeDeallocated message)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Region {region} for shard {message.Shard} not registered", "e");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_UnknownEntityRecoveryStrategy(string entityRecoveryStrategy)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unknown 'entity-recovery-strategy' [{entityRecoveryStrategy}], valid values are 'all' or 'constant'");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_Serializer_ClusterShardingMessage(object obj)
        {
            return new ArgumentException($"Can't serialize object of type [{(obj as Type) ?? obj.GetType()}] in [{nameof(ClusterShardingMessageSerializer)}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static SerializationException GetSerializationException_Serializer_ClusterShardingMessage(string manifest)
        {
            return new SerializationException($"Unimplemented deserialization of message with manifest [{manifest}] in [{nameof(ClusterShardingMessageSerializer)}]");
        }

        #endregion

        #region -- ArgumentNullException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_StartRrequiresTypeNameToBeProvided()
        {
            throw GetException();

            static ArgumentNullException GetException()
            {
                return new ArgumentNullException("typeName", "ClusterSharding start requires type name to be provided");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_StartRequiresPropsForTypeNameToBeProvided(string typeName)
        {
            throw GetException();
            ArgumentNullException GetException()
            {
                return new ArgumentNullException("entityProps", $"ClusterSharding start requires Props for [{typeName}] to be provided");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_ClusterShardingStartProxyRequiresTypeNameToBeProvided()
        {
            throw GetException();

            static ArgumentNullException GetException()
            {
                return new ArgumentNullException("typeName", "ClusterSharding start proxy requires type name to be provided");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_RequiresTunningParametersToBeProvided()
        {
            throw GetException();

            static ArgumentNullException GetException()
            {
                return new ArgumentNullException("tunningParameters", $"ClusterShardingSettings requires tunningParameters to be provided");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_RequiresCoordinatorSingletonSettingsToBeProvided()
        {
            throw GetException();

            static ArgumentNullException GetException()
            {
                return new ArgumentNullException("coordinatorSingletonSettings", $"ClusterShardingSettings requires coordinatorSingletonSettings to be provided");
            }
        }

        #endregion

        #region -- InvalidOperationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_MessageBuffersContainsId(string id)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"Message buffers contains id [{id}].");
            }
        }

        #endregion

        #region -- IllegalStateException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_UnexpectedChangeOfShard(PersistentShardCoordinator.ShardHome home)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"Unexpected change of shard [{home.Shard}] from self to [{home.Ref}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_ShardMustNotBeAllocatedToAProxyOnlyShardRegion()
        {
            throw GetException();

            static IllegalStateException GetException()
            {
                return new IllegalStateException("Shard must not be allocated to a proxy only ShardRegion");
            }
        }

        #endregion

        #region -- NotSupportedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException_DDataShardSendUpdateEventNotSupported(Shard.StateChange e)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException($"DDataShard send update event not supported: {e}");
            }
        }

        #endregion

        #region -- ActorInitializationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowActorInitialization_UnsupportedGuardianResponse(object reply)
        {
            throw GetException();
            ActorInitializationException GetException()
            {
                return new ActorInitializationException($"Unsupported guardian response: {reply}");
            }
        }

        #endregion
    }
}
