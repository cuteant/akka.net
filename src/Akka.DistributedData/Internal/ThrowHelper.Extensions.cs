using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Cluster;
using Akka.Configuration;
using Akka.Pattern;

namespace Akka.DistributedData
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        underlying,
        delta,
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
        internal static void ThrowArgumentException_ORDictionaryRemoveDeltaOpMustContainORSetRemoveDeltaOpInside()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("ORDictionary.RemoveDeltaOp must contain ORSet.RemoveDeltaOp inside");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ORDictionarySetItemsMayNotBeUsedToReplaceAnExistingORSet()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("ORDictionary.SetItems may not be used to replace an existing ORSet", "value");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_GroupDeltaShouldNotBeNested()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("GroupDelta should not be nested");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ClusterNodeMustNotBeTerminated()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("Cluster node must not be terminated");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_TryingToMergeTwoORMultiValueDictionaries()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Trying to merge two ORMultiValueDictionaries of different map sub-types");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_CapacityMustBe2_32()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("Capacity must be power of 2 and less than or equal 32", "capacity");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_CapacityMustBeLessThanOrEqual32()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("Capacity must be less than or equal 32", "capacity");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_WrongKeyUsedMustBeContainedKey()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("Wrong key used, must be contained key");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_WriteToRequiresCount()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("WriteTo requires count > 2, Use WriteLocal for count=1");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_StoreActorClassMustBeSetWhenDataDurableKeysHaveBeenConfigured()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"`akka.cluster.distributed-data.durable.store-actor-class` must be set when `akka.cluster.distributed-data.durable.keys` have been configured.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int ThrowArgumentException_ReadAggregatorDoesNotSupportReadLocal()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("ReadAggregator does not support ReadLocal");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int ThrowArgumentException_InvalidConsistencyLevel()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("Invalid consistency level");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static int ThrowArgumentException_WriteAggregatorDoesNotSupportWriteLocal()
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException("WriteAggregator does not support WriteLocal");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_CantPruneSinceItsNotFoundInDataEnvelope(UniqueAddress from)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Can't prune {@from} since it's not found in DataEnvelope");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ExpectedIDeltaReplicatedDataButGot(IReplicatedData data)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Expected {nameof(IDeltaReplicatedData)} but got '{data}' instead.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static IReplicatedData ThrowArgumentException_UnknownDeltaOperationOfType(IReplicatedData other)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Unknown delta operation of type {other.GetType()}", nameof(other));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_RemoveDeltaOperationShouldContainOneRemovedElement<T>(ORSet<T> underlying)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"RemoveDeltaOperation should contain one removed element, but was {underlying}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ORSet<T> ThrowArgumentException_CannotMergeDeltaOfType<T>(ORSet<T>.IDeltaOperation delta)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"Cannot merge delta of type {delta.GetType()}", nameof(delta));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_TheClusterNodeDoesNotHaveTheRole(Address selfAddress, ReplicatorSettings settings)
        {
            throw GetArgumentException();
            ArgumentException GetArgumentException()
            {
                return new ArgumentException($"The cluster node {selfAddress} does not have the role {settings.Role}");
            }
        }

        #endregion

        #region -- ArgumentNullException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_DistributedDataConfigNotProvided()
        {
            throw GetException();
            ArgumentNullException GetException()
            {
                return new ArgumentNullException("config", "DistributedData HOCON config not provided.");
            }
        }

        #endregion

        #region -- NotSupportedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ImmutableDictionary<T, VersionVector> ThrowNotSupportedException<T>()
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException();
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException_ReceivedResponseIdAndRequestCorrelationIdAreDifferent(UpdateSuccess success, Guid id)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException($"Received response id [{success.Request}] and request correlation id [{id}] are different.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException_ReceivedResponseIdAndRequestCorrelationIdAreDifferent(GetSuccess success, Guid id)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException($"Received response id [{success.Request}] and request correlation id [{id}] are different.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException_ReceivedResponseIdAndRequestCorrelationIdAreDifferent(DeleteSuccess success, Guid id)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException($"Received response id [{success.Request}] and request correlation id [{id}] are different.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException_UnknownResponseType(object response)
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException("Unknown response type: " + response);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static VersionVector ThrowNotSupportedException_CannotSubtractDotsFromProvidedVersionVector()
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException("Cannot subtract dots from provided version vector");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static VersionVector ThrowNotSupportedException_MultiVersionVectorDoesnotSupportMerge()
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException("MultiVersionVector doesn't support merge with provided version vector");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static VersionVector ThrowNotSupportedException_SingleVersionVectorDoesnotSupportMerge()
        {
            throw GetException();
            NotSupportedException GetException()
            {
                return new NotSupportedException("SingleVersionVector doesn't support merge with provided version vector");
            }
        }

        #endregion

        #region -- ConfigurationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_DistributedDataConfigNotProvided()
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException("HOCON config section `akka.cluster.distributed-data` was not found");
            }
        }

        #endregion

        #region -- InvalidCastException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T ThrowInvalidCastException_KeyTypeCannotBeCasted<T>(IKey key, IReplicatedData data, IKey<T> key0)
            where T : IReplicatedData
        {
            throw GetException();
            InvalidCastException GetException()
            {
                return new InvalidCastException($"Response returned for key '{key}' is of type [{data?.GetType()}] and cannot be casted using key '{key0}' to type [{typeof(T)}]");
            }
        }

        #endregion

        #region -- IllegalStateException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_MissingValueFor<TKey>(TKey key)
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException($"Missing value for {key}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowIllegalStateException_CannotNestDeltaGroup()
        {
            throw GetException();
            IllegalStateException GetException()
            {
                return new IllegalStateException("Cannot nest DeltaGroup");
            }
        }

        #endregion

        #region -- TimeoutException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTimeoutException_CouldnotRetrieveTheDataUnderKey<T>(IKey<T> key, IReadConsistency consistency)
            where T : IReplicatedData
        {
            throw GetException();
            TimeoutException GetException()
            {
                return new TimeoutException($"Couldn't retrieve the data under key [{key}] within consistency constraints {consistency} and under provided timeout.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTimeoutException_CouldnotConfirmUpdateOfTheDataUnderKey<T>(IKey<T> key, IWriteConsistency consistency)
            where T : IReplicatedData
        {
            throw GetException();
            TimeoutException GetException()
            {
                return new TimeoutException($"Couldn't confirm update of the data under key [{key}] within consistency constraints {consistency} and under provided timeout.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowTimeoutException_CouldnotConfirmDeletionOfTheDataUnderKey<T>(IKey<T> key, IWriteConsistency consistency)
            where T : IReplicatedData
        {
            throw GetException();
            TimeoutException GetException()
            {
                return new TimeoutException($"Couldn't confirm deletion of the data under key [{key}] within consistency constraints {consistency} and under provided timeout.");
            }
        }

        #endregion

        #region -- DataDeletedException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowDataDeletedException_CannotRetrieveDataUnderK1ey<T>(IKey<T> key)
            where T : IReplicatedData
        {
            throw GetException();
            DataDeletedException GetException()
            {
                return new DataDeletedException($"Cannot retrieve data under key [{key}]. It has been permanently deleted and the key cannot be reused.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowDataDeletedException_CannotStoreDataUnderKey<T>(IKey<T> key)
            where T : IReplicatedData
        {
            throw GetException();
            DataDeletedException GetException()
            {
                return new DataDeletedException($"Cannot store data under key [{key}]. It has been permanently deleted and the key cannot be reused.");
            }
        }

        #endregion
    }
}
