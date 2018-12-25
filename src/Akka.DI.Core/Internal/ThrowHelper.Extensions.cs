using System;
using System.Runtime.CompilerServices;

namespace Akka.DI.Core
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
        container,
        system,
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
        internal static void ThrowArgumentNullException_RequiresSystem()
        {
            throw GetArgumentNullException();
            ArgumentNullException GetArgumentNullException()
            {
                return new ArgumentNullException("system", "ActorSystem requires a valid system");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_RequiresDR()
        {
            throw GetArgumentNullException();
            ArgumentNullException GetArgumentNullException()
            {
                return new ArgumentNullException("dependencyResolver", "ActorSystem requires dependencyResolver to be provided");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_DIExt()
        {
            throw GetArgumentNullException();
            ArgumentNullException GetArgumentNullException()
            {
                return new ArgumentNullException("dependencyResolver", $"DIExt requires dependencyResolver to be provided");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_DIActorSystemAdapter()
        {
            throw GetArgumentNullException();
            ArgumentNullException GetArgumentNullException()
            {
                return new ArgumentNullException("system", $"DIActorSystemAdapter requires system to be provided");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_DIActorProducer_Type()
        {
            throw GetArgumentNullException();
            ArgumentNullException GetArgumentNullException()
            {
                return new ArgumentNullException("actorType", $"DIActorProducer requires actorType to be provided");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_DIActorProducer_DR()
        {
            throw GetArgumentNullException();
            ArgumentNullException GetArgumentNullException()
            {
                return new ArgumentNullException("dependencyResolver", $"DIActorProducer requires dependencyResolver to be provided");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowArgumentNullException_Context()
        {
            throw GetArgumentNullException();
            ArgumentNullException GetArgumentNullException()
            {
                return new ArgumentNullException("context", $"DIActorContextAdapter requires context to be provided");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidOperationException_TheDependencyResolverHasNotBeenConfiguredYet()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("The Dependency Resolver has not been configured yet");
            }
        }
    }
}
