﻿//-----------------------------------------------------------------------
// <copyright file="SuspendReason.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using MessagePack;

namespace Akka.Actor.Internal
{
    /// <summary>
    /// TBD
    /// <remarks>Note! Part of internal API. Breaking changes may occur without notice. Use at own risk.</remarks>
    /// </summary>
    public abstract class SuspendReason
    {
        /// <summary>
        /// TBD
        /// <remarks>Note! Part of internal API. Breaking changes may occur without notice. Use at own risk.</remarks>
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public interface IWaitingForChildren
        {
            //Intentionally left blank
        }

        /// <summary>
        /// TBD
        /// <remarks>Note! Part of internal API. Breaking changes may occur without notice. Use at own risk.</remarks>
        /// </summary>
        [MessagePackObject]
        public class Creation : SuspendReason, IWaitingForChildren
        {
            //Intentionally left blank
        }

        /// <summary>
        /// TBD
        /// <remarks>Note! Part of internal API. Breaking changes may occur without notice. Use at own risk.</remarks>
        /// </summary>
        [MessagePackObject]
        public class Recreation : SuspendReason, IWaitingForChildren
        {
            /// <summary>
            /// TBD
            /// </summary>
            /// <param name="cause">TBD</param>
            public Recreation(Exception cause) => Cause = cause;

            /// <summary>
            /// TBD
            /// </summary>
            [Key(0)]
            public Exception Cause { get; }
        }

        /// <summary>
        /// TBD
        /// <remarks>Note! Part of internal API. Breaking changes may occur without notice. Use at own risk.</remarks>
        /// </summary>
        public class Termination : SuspendReason, ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly Termination Instance = new Termination();
            private Termination() { }
        }

        /// <summary>
        /// TBD
        /// <remarks>Note! Part of internal API. Breaking changes may occur without notice. Use at own risk.</remarks>
        /// </summary>
        public class UserRequest : SuspendReason, ISingletonMessage
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly UserRequest Instance = new UserRequest();
            private UserRequest() { }
        }
    }
}

