using System;
using System.Runtime.CompilerServices;
using Akka.Cluster.Sharding.Serialization;

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
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static T ThrowArgumentException_Serializer_ClusterShardingMessage<T>(object obj)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Can't serialize object of type [{obj.GetType()}] in [{nameof(ClusterShardingMessageSerializer)}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static object ThrowArgumentException_Serializer_ClusterShardingMessage(string manifest)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in [{nameof(ClusterShardingMessageSerializer)}]");
            }
        }
    }
}
