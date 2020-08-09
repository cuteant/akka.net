using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Akka.Internal;

namespace Akka
{
  internal static partial class AkkaThrowHelper
  {
    #region -- Throw ArgumentException --

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentException()
    {
      throw GetArgumentException();

            static ArgumentException GetArgumentException()
      {
        return new ArgumentException();
      }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentException(AkkaExceptionResource resource)
    {
      throw GetArgumentException(resource);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentException(AkkaExceptionResource resource, AkkaExceptionArgument argument)
    {
      throw GetArgumentException(resource, argument);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentException(string message, AkkaExceptionArgument argument)
    {
      throw GetArgumentException();
      ArgumentException GetArgumentException()
      {
        return new ArgumentException(message, GetArgumentName(argument));

      }
    }

    #endregion

    #region -- Get ArgumentException --

    internal static ArgumentException GetArgumentException(AkkaExceptionResource resource)
    {
      return new ArgumentException(GetResourceString(resource));
    }

    internal static ArgumentException GetArgumentException(AkkaExceptionResource resource, AkkaExceptionArgument argument)
    {
      return new ArgumentException(GetResourceString(resource), GetArgumentName(argument));
    }

    #endregion


    #region -- Throw ArgumentOutOfRangeException --

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentOutOfRangeException()
    {
      throw GetArgumentOutOfRangeException();

            static ArgumentOutOfRangeException GetArgumentOutOfRangeException()
      {
        return new ArgumentOutOfRangeException();
      }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentOutOfRangeException(AkkaExceptionArgument argument)
    {
      throw GetArgumentOutOfRangeException(argument);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentOutOfRangeException(AkkaExceptionArgument argument, AkkaExceptionResource resource)
    {
      throw GetArgumentOutOfRangeException(argument, resource);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentOutOfRangeException(AkkaExceptionArgument argument, int paramNumber, AkkaExceptionResource resource)
    {
      throw GetArgumentOutOfRangeException(argument, paramNumber, resource);
    }

    #endregion

    #region -- Get ArgumentOutOfRangeException --

    internal static ArgumentOutOfRangeException GetArgumentOutOfRangeException(AkkaExceptionArgument argument)
    {
      return new ArgumentOutOfRangeException(GetArgumentName(argument));
    }

    internal static ArgumentOutOfRangeException GetArgumentOutOfRangeException(AkkaExceptionArgument argument, AkkaExceptionResource resource)
    {
      return new ArgumentOutOfRangeException(GetArgumentName(argument), GetResourceString(resource));
    }

    internal static ArgumentOutOfRangeException GetArgumentOutOfRangeException(AkkaExceptionArgument argument, int paramNumber, AkkaExceptionResource resource)
    {
      return new ArgumentOutOfRangeException(GetArgumentName(argument) + "[" + paramNumber.ToString() + "]", GetResourceString(resource));
    }

    #endregion


    #region -- Throw ArgumentNullException --

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentNullException(AkkaExceptionArgument argument)
    {
      throw GetArgumentNullException(argument);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentNullException(AkkaExceptionResource resource)
    {
      throw GetArgumentNullException(resource);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowArgumentNullException(AkkaExceptionArgument argument, AkkaExceptionResource resource)
    {
      throw GetArgumentNullException(argument, resource);
    }

    #endregion

    #region -- Get ArgumentNullException --

    internal static ArgumentNullException GetArgumentNullException(AkkaExceptionArgument argument)
    {
      return new ArgumentNullException(GetArgumentName(argument));
    }

    internal static ArgumentNullException GetArgumentNullException(AkkaExceptionResource resource)
    {
      return new ArgumentNullException(GetResourceString(resource), innerException: null);
    }

    internal static ArgumentNullException GetArgumentNullException(AkkaExceptionArgument argument, AkkaExceptionResource resource)
    {
      return new ArgumentNullException(GetArgumentName(argument), GetResourceString(resource));
    }

    #endregion


    #region -- IndexOutOfRangeException --

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowIndexOutOfRangeException()
    {
      throw GetIndexOutOfRangeException();

            static IndexOutOfRangeException GetIndexOutOfRangeException()
      {
        return new IndexOutOfRangeException();
      }
    }

    #endregion

    #region -- Throw InvalidOperationException --

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowInvalidOperationException(AkkaExceptionResource resource)
    {
      throw GetInvalidOperationException(resource);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal static void ThrowInvalidOperationException(AkkaExceptionResource resource, Exception e)
    {
      throw GetInvalidOperationException();
      InvalidOperationException GetInvalidOperationException()
      {
        return new InvalidOperationException(GetResourceString(resource), e);
      }
    }

    internal static InvalidOperationException GetInvalidOperationException(AkkaExceptionResource resource)
    {
      return new InvalidOperationException(GetResourceString(resource));
    }

    #endregion

    #region ** GetArgumentName **

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GetArgumentName(AkkaExceptionArgument argument)
    {
      Debug.Assert(Enum.IsDefined(typeof(AkkaExceptionArgument), argument),
          "The enum value is not defined, please check the ExceptionArgument Enum.");

      return argument.ToString();
    }

    #endregion

    #region ** GetResourceString **

    // This function will convert an ExceptionResource enum value to the resource string.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static string GetResourceString(AkkaExceptionResource resource)
    {
      Debug.Assert(Enum.IsDefined(typeof(AkkaExceptionResource), resource),
          "The enum value is not defined, please check the ExceptionResource Enum.");

      return SR.GetResourceString(resource.ToString());
    }

    #endregion
  }
}
