﻿//-----------------------------------------------------------------------
// <copyright file="TypeExtensions.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Concurrent;
using System.Reflection;
using Akka.Annotations;
using CuteAnt.Reflection;

namespace Akka.Util
{
    /// <summary>
    /// Class TypeExtensions.
    /// </summary>
    public static class TypeExtensions
    {
        /// <summary>
        /// Returns true if <paramref name="type" /> implements/inherits <typeparamref name="T" />.
        /// <example><para>typeof(object[]).Implements&lt;IEnumerable&gt;() --&gt; true</para></example>
        /// </summary>
        /// <typeparam name="T">TBD</typeparam>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool Implements<T>(this Type type) => Implements(type, typeof(T));

        /// <summary>
        /// Returns true if <paramref name="type" /> implements/inherits <paramref name="moreGeneralType" />.
        /// <example><para>typeof(object[]).Implements(typeof(IEnumerable)) --&gt; true</para></example>
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="moreGeneralType">Type of the more general.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public static bool Implements(this Type type, Type moreGeneralType) => moreGeneralType.IsAssignableFrom(type);

        /// <summary>
        /// INTERNAL API
        /// Utility to be used by implementers to create a manifest from the type.
        /// The manifest is used to look up the type on deserialization.
        /// </summary>
        /// <param name="type">TBD</param>
        /// <returns>Returns the type qualified name including namespace and assembly, but not assembly version.</returns>
        [InternalApi]
        public static string TypeQualifiedName(this Type type) => RuntimeTypeNameFormatter.Format(type);
    }

    public static class TypeUtil
    {
        public static Type ResolveType(string qualifiedTypeName, bool throwOnError = false)
        {
            try
            {
                if (TypeUtils.TryResolveType(qualifiedTypeName, out var result)) { return result; }
            }
            catch { }
            if (throwOnError)
            {
                AkkaThrowHelper.ThrowTypeLoadException_UnableToFindATypeNamed(qualifiedTypeName);
            }
            return null;
        }
    }
}

