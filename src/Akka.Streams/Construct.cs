﻿//-----------------------------------------------------------------------
// <copyright file="Construct.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using CuteAnt.Reflection;

namespace Akka.Streams
{
    /// <summary>
    /// TBD
    /// </summary>
    public static class Construct
    {
        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="genericType">TBD</param>
        /// <param name="genericParam">TBD</param>
        /// <returns>TBD</returns>
        public static object Instantiate(this Type genericType, Type genericParam)
        {
            var gen = genericType.GetCachedGenericType(genericParam);
            return ActivatorUtils.FastCreateInstance(gen);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="genericType">TBD</param>
        /// <param name="genericParam">TBD</param>
        /// <param name="constructorArgs">TBD</param>
        /// <returns>TBD</returns>
        public static object Instantiate(this Type genericType, Type genericParam, params object[] constructorArgs)
        {
            var gen = genericType.GetCachedGenericType(genericParam);
            return Activator.CreateInstance(gen, constructorArgs);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="genericType">TBD</param>
        /// <param name="genericParams">TBD</param>
        /// <returns>TBD</returns>
        public static object Instantiate(this Type genericType, Type[] genericParams)
        {
            var gen = genericType.GetCachedGenericType(genericParams);
            return ActivatorUtils.FastCreateInstance(gen);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="genericType">TBD</param>
        /// <param name="genericParams">TBD</param>
        /// <param name="constructorArgs">TBD</param>
        /// <returns>TBD</returns>
        public static object Instantiate(this Type genericType, Type[] genericParams, params object[] constructorArgs)
        {
            var gen = genericType.GetCachedGenericType(genericParams);
            return Activator.CreateInstance(gen, constructorArgs);
        }
    }
}
