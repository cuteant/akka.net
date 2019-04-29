using System;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Cluster.Serialization;
using Akka.Configuration;
using Akka.Routing;
using Akka.Util;

namespace Akka.Cluster
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        strategy,
        uniqueAddress,
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
        x,
        y,
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
        #region -- Exception --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowException_NoYoungestWhenNoMembers()
        {
            throw GetException();
            Exception GetException()
            {
                return new Exception("No youngest when no members");
            }
        }

        #endregion

        #region -- ArgumentException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_RouteesPathsMustBeDefined()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("RouteesPaths must be defined", "routeesPaths");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_RouteesPathsIsNotAValidRelativeActorPath(string path)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"routeesPaths [{path}] is not a valid relative actor path.", "routeesPaths");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_JoinSeedNodeShouldNotBeDone()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("Join seed node should not be done");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_MemberCompare(object obj)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Cannot compare {nameof(Member)} to an instance of type '{obj?.GetType().FullName ?? "null"}'");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_UniqueAddressCompare(object obj)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Cannot compare {nameof(UniqueAddress)} with instance of type '{obj?.GetType().FullName ?? "null"}'.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_UnknownInClusterMessage<T>(T value, string unknown)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Unknown {unknown} [{value}] in cluster message");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ExpectedUpOrRemovedInOnMemberStatusChangedListener(MemberStatus status)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Expected Up or Removed in OnMemberStatusChangedListener, got [{status}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_Serializer_ClusterMessage(object obj)
        {
            return new ArgumentException($"Can't serialize object of type [{obj.GetType()}] in [{nameof(ClusterMessageSerializer)}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_Serializer_ClusterMessage(string manifest)
        {
            return new ArgumentException($"Unimplemented deserialization of message with manifest [{manifest}] in [{nameof(ClusterMessageSerializer)}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ArgumentException GetArgumentException_Serializer_D(object obj)
        {
            var type = obj as Type;
            var typeQualifiedName = type != null ? type.TypeQualifiedName() : obj?.GetType().TypeQualifiedName();
            return new ArgumentException($"Cannot deserialize object of type [{typeQualifiedName}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_AtLeastOneIClusterDomainEventClassIsRequired()
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException("At least one `IClusterDomainEvent` class is required", "to");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_SubscribeToIClusterDomainEventOrSubclasses(Type[] to)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Subscribe to `IClusterDomainEvent` or subclasses, was [{string.Join(", ", to.Select(c => c.Name))}]", nameof(to));
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ClusterAwareRouterCanOnlyWrapPoolOrGroup(Deploy deploy)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Cluster-aware router can only wrap Pool or Group, got [{deploy.RouterConfig.GetType()}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ExpectedState(Member member, MemberStatus validStatus)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Expected {validStatus} state, got: {member}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ExpectedRemoveStatus(Member member)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Expected Removed status, got {member}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ResolveSplitBrainStrategy(string activeStrategy)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"`akka.cluster.split-brain-resolver.active-strategy` setting not recognized: [{activeStrategy}]. Available options are: static-quorum, keep-majority, keep-oldest, keep-referee.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_NodesMustContainSelfAddress(UniqueAddress selfAddress, ImmutableHashSet<UniqueAddress> nodes)
        {
            throw GetException();
            ArgumentException GetException()
            {
                return new ArgumentException($"Nodes [${string.Join(", ", nodes)}] must contain selfAddress [{selfAddress}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_ExpectedRemoveStatus(ImmutableSortedSet<Member> members)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var mems = string.Join(", ", members.Where(m => m.Status == MemberStatus.Removed).Select(m => m.ToString()));
                return new ArgumentException($"Live members must not have status [Removed], got {mems}", "Members");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_NodesNotPartOfClusterInReachabilityTable(ImmutableHashSet<UniqueAddress> inReachabilityButNotMember)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var inreachability = string.Join(", ", inReachabilityButNotMember.Select(a => a.ToString()));
                return new ArgumentException($"Nodes not part of cluster in reachability table, got {inreachability}", "Overview");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentException_NodesNotPartOfClusterHaveMarkedTheGossipAsSeen(ImmutableHashSet<UniqueAddress> seenButNotMember)
        {
            throw GetException();
            ArgumentException GetException()
            {
                var seen = string.Join(", ", seenButNotMember.Select(a => a.ToString()));
                return new ArgumentException($"Nodes not part of cluster have marked the Gossip as seen, got {seen}", "Overview");
            }
        }

        #endregion

        #region -- ArgumentOutOfRangeException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_MaxInstancesPerNode()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("maxInstancesPerNode", "maxInstancesPerNode of cluster pool router must be > 0");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_UseRole()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("useRole", "useRole must be either null or non-empty");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentOutOfRangeException_TotalInstances()
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("totalInstances", "totalInstances of cluster router must be > 0");
            }
        }

        #endregion

        #region -- InvalidOperationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_SequenceWasEmpty()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Sequence was empty");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_MustBeLeaderToDownNode()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Must be leader to down node");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_JoinCanOnlyBeDoneFromAnEmptyState()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Join can only be done from an empty state");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_WelcomeCanOnlyBeDoneFromAnEmptyState()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Welcome can only be done from an empty state");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_TooManyVectorClockEntriesInGossipState(Gossip latestGossip)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"Too many vector clock entries in gossip state {latestGossip}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_MemberCopy(MemberStatus x, MemberStatus y)
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException($"Invalid member status transition {x} -> {y}");
            }
        }

        #endregion

        #region -- ConfigurationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static ConfigurationException GetConfigurationException_CouldnotCreateDowningProviderOfType(Type downingProviderType, Exception e)
        {
            return new ConfigurationException($"Couldn't create downing provider of type [{downingProviderType.FullName}]", e);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_ClusterrouterpoolIsNotAllowedToWrap()
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException("ClusterRouterPool is not allowed to wrap a ClusterRouterPool");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_ClusterroutergroupIsNotAllowedToWrap()
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException("ClusterRouterGroup is not allowed to wrap a ClusterRouterGroup");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_ResizerCanotBeUsedTogetherWithClusterRouter()
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException("Resizer can't be used together with cluster router.");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_AutoDowningDowningProviderSelected()
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException("AutoDowning downing provider selected but 'akka.cluster.auto-down-unreachable-after' not set");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_ClusterDeploymentCanotBeCombinedWithScope(Deploy deploy)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Cluster deployment can't be combined with scope [{deploy.Scope}]");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowConfigurationException_ClusterDeploymentCanotBeCombinedWith(Deploy deploy)
        {
            throw GetException();
            ConfigurationException GetException()
            {
                return new ConfigurationException($"Cluster deployment can't be combined with [{deploy.Config}]");
            }
        }

        #endregion

        #region -- ActorInitializationException --

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowActorInitializationException_ClusterRouterActorCanOnlyBeUsedWithPoolOrGroup(RouterConfig routerConfig)
        {
            throw GetException();
            ActorInitializationException GetException()
            {
                return new ActorInitializationException(
                    $"Cluster router actor can only be used with Pool or Group, not with {routerConfig.GetType()}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowActorInitializationException_ClusterroutergroupactorCanOnlyBeUsedWithGroup(RouterConfig routerConfig)
        {
            throw GetException();
            ActorInitializationException GetException()
            {
                return new ActorInitializationException(
                    $"ClusterRouterGroupActor can only be used with group, not {routerConfig.GetType()}");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowActorInitializationException_RouterpoolactorCanOnlyBeUsedWithPool(RouterConfig routerConfig)
        {
            throw GetException();
            ActorInitializationException GetException()
            {
                return new ActorInitializationException(
                    $"RouterPoolActor can only be used with Pool, not {routerConfig.GetType()}");
            }
        }

        #endregion
    }
}
