using System;
using System.Runtime.CompilerServices;
using Akka.Cluster.Tools.Client.Serialization;
using Akka.Cluster.Tools.PublishSubscribe.Serialization;
using Akka.Cluster.Tools.Singleton.Serialization;

namespace Akka.Cluster.Tools
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
        internal static object ThrowArgumentException_Serializer_ClusterClientMessage(string manifest)
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
        internal static object ThrowArgumentException_Serializer_DistributedPubSubMessage(string manifest)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in serializer {nameof(DistributedPubSubMessageSerializer)}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static object ThrowArgumentException_Serializer_ClusterSingletonMessage(string manifest)
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
                return new ArgumentException($"Can't serialize object of type [{o.GetType()}] in [{nameof(ClusterClientMessageSerializer)}]");
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
    }
}
