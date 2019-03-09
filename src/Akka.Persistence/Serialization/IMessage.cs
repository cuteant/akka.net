﻿//-----------------------------------------------------------------------
// <copyright file="IMessage.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Serialization;
using Akka.Util;

namespace Akka.Persistence.Serialization
{
    /// <summary>Marked interface used to identify message types which are used in persistence.
    /// <see cref="IPersistentRepresentation"/>.</summary>
    public interface IMessage { }

    /// <summary>Marked interface used to identify message types which are used in persistence.
    /// <see cref="Akka.Persistence.Fsm.PersistentFSM.IPersistentFSMSnapshot"/>.</summary>
    public interface IFSMSnapshotMessage { }
}
