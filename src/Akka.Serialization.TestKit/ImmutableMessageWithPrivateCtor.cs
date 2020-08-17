﻿//-----------------------------------------------------------------------
// <copyright file="ImmutableMessageWithPrivateCtor.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;

namespace Akka.Tests.Serialization
{
    public class ImmutableMessageWithPrivateCtor
    {
        public string Foo { get; private set; }
        public string Bar { get; private set; }

        protected ImmutableMessageWithPrivateCtor()
        {
        }

        protected bool Equals(ImmutableMessageWithPrivateCtor other)
        {
            return String.Equals(Bar, other.Bar) && String.Equals(Foo, other.Foo);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ImmutableMessageWithPrivateCtor)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Bar is object ? Bar.GetHashCode() : 0) * 397) ^ (Foo is object ? Foo.GetHashCode() : 0);
            }
        }

        public ImmutableMessageWithPrivateCtor(Tuple<string, string> nonConventionalArg)
        {
            Foo = nonConventionalArg.Item1;
            Bar = nonConventionalArg.Item2;
        }
    }
}
