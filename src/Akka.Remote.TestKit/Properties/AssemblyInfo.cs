﻿//-----------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if SIGNED
[assembly: InternalsVisibleTo("Akka.Remote.TestKit.Tests, PublicKey=00240000048000009400000006020000002400005253413100040000010001003df2cefc3e3c196195f046768979f5998131a23270da7485c84d0e46175140c4227e93fe392829d51d1e1ffbe0d6edb3bb0b2b05556f829f2f1a184f23ce052e2b2134ba0ae7aa9143a7959cea16accb18d1417bf48dabac10c2c0828ede943c5960e85713ca29eea555959ea6dbdd41d1000bf62da370883c4dc5c3508a22df")]
#else
[assembly: InternalsVisibleTo("Akka.Remote.TestKit.Tests")]
#endif
