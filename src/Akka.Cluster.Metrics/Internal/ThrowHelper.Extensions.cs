using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Pattern;

namespace Akka.Cluster.Metrics
{
    #region -- ExceptionArgument --

    /// <summary>The convention for this enum is using the argument name as the enum name</summary>
    internal enum ExceptionArgument
    {
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
    }
}
