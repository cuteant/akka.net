﻿//-----------------------------------------------------------------------
// <copyright file="Deployer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Akka.Configuration;
using Akka.Routing;
using Akka.Util;
using Akka.Util.Internal;

namespace Akka.Actor
{
    /// <summary>
    /// Used to configure and deploy actors.
    /// </summary>
    public class Deployer
    {
        /// <summary>
        /// TBD
        /// </summary>
        protected readonly Config Default;
        private readonly Settings _settings;
        private readonly AtomicReference<WildcardIndex<Deploy>> _deployments =
            new AtomicReference<WildcardIndex<Deploy>>(new WildcardIndex<Deploy>());

        /// <summary>
        /// Initializes a new instance of the <see cref="Deployer"/> class.
        /// </summary>
        /// <param name="settings">The settings used to configure the deployer.</param>
        public Deployer(Settings settings)
        {
            const string defaultKey = "default";

            _settings = settings;
            var config = settings.Config.GetConfig("akka.actor.deployment");
            Default = config.GetConfig(defaultKey);

            if (config.IsNullOrEmpty()) { return; }

            var rootObj = config.Root.GetObject();
            //if (rootObj is null) return;
#if NETCOREAPP_2_X_GREATER || NETSTANDARD_2_0_GREATER
            var unwrapped = rootObj.Unwrapped.Where(d => !string.Equals(defaultKey, d.Key)).ToArray();
#else
            var unwrapped = rootObj.Unwrapped.Where(d => !string.Equals(defaultKey, d.Key, StringComparison.Ordinal)).ToArray();
#endif
            foreach (var d in unwrapped.Select(x => ParseConfig(x.Key, config.GetConfig(x.Key.BetweenDoubleQuotes()))))
            {
                SetDeploy(d);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="path">TBD</param>
        /// <returns>TBD</returns>
        public Deploy Lookup(ActorPath path)
        {
            const string _user = "user";

            var rawElements = path.Elements;
#if NETCOREAPP_2_X_GREATER || NETSTANDARD_2_0_GREATER
            if (!string.Equals(_user, rawElements[0]) || rawElements.Count < 2)
#else
            if (!string.Equals(_user, rawElements[0], StringComparison.Ordinal) || rawElements.Count < 2)
#endif
            {
                return Deploy.None;
            }

            var elements = rawElements.Drop(1);
            return Lookup(elements);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="path">TBD</param>
        /// <returns>TBD</returns>
        public Deploy Lookup(IEnumerable<string> path)
        {
            return _deployments.Value.Find(path);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="deploy">TBD</param>
        /// <exception cref="IllegalActorNameException">
        /// This exception is thrown if the actor name in the deployment path is empty or contains invalid ASCII.
        /// Valid ASCII includes letters and anything from <see cref="ActorPath.ValidSymbols"/>. Note that paths
        /// cannot start with the <c>$</c>.
        /// </exception>
        public void SetDeploy(Deploy deploy)
        {
            void add(IList<string> path, Deploy d)
            {
                var w = _deployments.Value;
                foreach (var t in path)
                {
                    if (string.IsNullOrEmpty(t)) { AkkaThrowHelper.ThrowIllegalActorNameException(d); }
                    if (!ActorPath.IsValidPathElement(t))
                    {
                        AkkaThrowHelper.ThrowIllegalActorNameException(t, d);
                    }
                }
                if (!_deployments.CompareAndSet(w, w.Insert(path, d))) add(path, d);
            }

            var elements = deploy.Path.Split('/').Drop(1).ToList();
            add(elements, deploy);
        }

        /// <summary>
        /// Creates an actor deployment to the supplied path, <paramref name="key"/>, using the supplied configuration, <paramref name="config"/>.
        /// </summary>
        /// <param name="key">The path used to deploy the actor.</param>
        /// <param name="config">The configuration used to configure the deployed actor.</param>
        /// <returns>A configured actor deployment to the given path.</returns>
        public virtual Deploy ParseConfig(string key, Config config)
        {
            var deployment = config.WithFallback(Default);
            var routerType = deployment.GetString("router", null);
            var router = CreateRouterConfig(routerType, deployment);
            var dispatcher = deployment.GetString("dispatcher", null);
            var mailbox = deployment.GetString("mailbox", null);
            var deploy = new Deploy(key, deployment, router, Deploy.NoScopeGiven, dispatcher, mailbox);
            return deploy;
        }

        private RouterConfig CreateRouterConfig(string routerTypeAlias, Config deployment)
        {
            if (routerTypeAlias == "from-code") { return NoRouter.Instance; }

            if (deployment.IsNullOrEmpty())
            {
                throw ConfigurationException.NullOrEmptyConfig<RouterConfig>();
            }

            var path = string.Format("akka.actor.router.type-mapping.{0}", routerTypeAlias);
            var routerTypeName = _settings.Config.GetString(path, null);
            var routerType = TypeUtil.ResolveType(routerTypeName);
            Debug.Assert(routerType is object, "routerType is object");
            var routerConfig = (RouterConfig)Activator.CreateInstance(routerType, deployment);

            return routerConfig;
        }
    }
}
