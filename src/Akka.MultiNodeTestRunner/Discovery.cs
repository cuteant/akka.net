﻿//-----------------------------------------------------------------------
// <copyright file="Discovery.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Akka.MultiNodeTestRunner.Shared;
using Akka.Remote.TestKit;
using Akka.Util;
using Akka.Util.Internal;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Akka.MultiNodeTestRunner
{
#if CORECLR
    public class Discovery : IMessageSink, IDisposable
#else
    public class Discovery : MarshalByRefObject, IMessageSink, IDisposable
#endif
    {
        public Dictionary<string, List<NodeTest>> Tests { get; set; }
        public List<ErrorMessage> Errors { get; } = new List<ErrorMessage>();
        public bool WasSuccessful => Errors.IsEmpty();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Discovery"/> class.
        /// </summary>
        public Discovery()
        {
            Tests = new Dictionary<string, List<NodeTest>>();
            Finished = new ManualResetEvent(false);
        }

        public ManualResetEvent Finished { get; private set; }
        public IMessageSink NextSink { get; private set; }

        public virtual bool OnMessage(IMessageSinkMessage message)
        {
            switch (message)
            {
                case ITestCaseDiscoveryMessage testCaseDiscoveryMessage:
                    var testClass = testCaseDiscoveryMessage.TestClass.Class;
                    if (testClass.IsAbstract) return true;
                    var details = LoadTestCaseDetails(testCaseDiscoveryMessage, testClass);
                    if (details.Any())
                    {
                        var dictKey = details.First().TestName;
                        if (Tests.ContainsKey(dictKey))
                            Tests[dictKey].AddRange(details);
                        else
                            Tests.Add(dictKey, details);
                    }
                    break;
                case IDiscoveryCompleteMessage discoveryComplete:
                    Finished.Set();
                    break;
                case ErrorMessage err:
                    Errors.Add(err);
                    break;
            }

            return true;
        }

        private List<NodeTest> LoadTestCaseDetails(ITestCaseDiscoveryMessage testCaseDiscoveryMessage, ITypeInfo testClass)
        {
            try
            {
#if CORECLR
                var specType = testCaseDiscoveryMessage.TestAssembly.Assembly.GetType(testClass.Name).ToRuntimeType();
#else
                var testAssembly = Assembly.LoadFrom(testCaseDiscoveryMessage.TestAssembly.Assembly.AssemblyPath);
                var specType = testAssembly.GetType(testClass.Name);
#endif
                var roles = RoleNames(specType);

                var details = roles.Select((r, i) => new NodeTest
                {
                    Node = i + 1,
                    Role = r.Name,
                    TestName = testClass.Name,
                    TypeName = testClass.Name,
                    MethodName = testCaseDiscoveryMessage.TestCase.TestMethod.Method.Name,
                    SkipReason = testCaseDiscoveryMessage.TestCase.SkipReason,
                }).ToList();

                return details;
            }
            catch (Exception ex)
            {
                // If something goes wrong with loading test details - just keep going with other tests
                Console.WriteLine($"Failed to load test details for [{testClass.Name}] test class: {ex}");
                return new List<NodeTest>();
            }
        }

        private IEnumerable<RoleName> RoleNames(Type specType)
        {
            var ctorWithConfig = FindConfigConstructor(specType);
            var configType = ctorWithConfig.GetParameters().First().ParameterType;
            var args = ConfigConstructorParamValues(configType);
            var configInstance = Activator.CreateInstance(configType, args);
            var roleType = typeof(RoleName);
            var configProps = configType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var roleProps = configProps.Where(p => p.PropertyType == roleType && p.Name != "Myself").Select(p => (RoleName)p.GetValue(configInstance));
            var configFields = configType.GetFields(BindingFlags.Instance | BindingFlags.Public);
            var roleFields = configFields.Where(f => f.FieldType == roleType && f.Name != "Myself").Select(f => (RoleName)f.GetValue(configInstance));
            var roles = roleProps.Concat(roleFields).Distinct();
            return roles;
        }

        internal static ConstructorInfo FindConfigConstructor(Type configUser)
        {
            var baseConfigType = typeof(MultiNodeConfig);
            var current = configUser;
            while (current is object)
            {
                var ctorWithConfig = current
                    .GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(c => null != c.GetParameters().FirstOrDefault(p => p.ParameterType.IsSubclassOf(baseConfigType)));
                if (ctorWithConfig is object) return ctorWithConfig;

                current = current.BaseType;
            }

            throw new ArgumentException($"[{configUser}] or one of its base classes must specify constructor, which first parameter is a subclass of {baseConfigType}");
        }

        private object[] ConfigConstructorParamValues(Type configType)
        {
            var ctors = configType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            var empty = ctors.FirstOrDefault(c => !c.GetParameters().Any());

            return empty is object
                ? new object[0]
                : ctors.First().GetParameters().Select(p => p.ParameterType.IsValueType ? Activator.CreateInstance(p.ParameterType) : null).ToArray();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Finished.Dispose();
        }
    }
}
