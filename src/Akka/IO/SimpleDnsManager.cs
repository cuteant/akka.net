//-----------------------------------------------------------------------
// <copyright file="SimpleDnsManager.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using Akka.Actor;
using Akka.Dispatch;
using Akka.Event;

namespace Akka.IO
{
    /// <summary>
    /// TBD
    /// </summary>
    public class SimpleDnsManager : ActorBase, IRequiresMessageQueue<IUnboundedMessageQueueSemantics>
    {
        private readonly DnsExt _ext;
        private readonly IActorRef _resolver;
        private IPeriodicCacheCleanup _cacheCleanup;
        private ICancelable _cleanupTimer;
#if DEBUG
        private readonly ILoggingAdapter _log = Context.GetLogger();
#endif

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="ext">TBD</param>
        public SimpleDnsManager(DnsExt ext)
        {
            _ext = ext;
            _resolver = Context.ActorOf(Props.Create(ext.Provider.ActorClass, ext.Cache, ext.Settings.ResolverConfig)
                                             .WithDeploy(Deploy.Local)
                                             .WithDispatcher(ext.Settings.Dispatcher));

            _cacheCleanup = _ext.Cache as IPeriodicCacheCleanup;

            if (_cacheCleanup is object)
            {
                var interval = ext.Settings.ResolverConfig.GetTimeSpan("cache-cleanup-interval", null);
                _cleanupTimer = Context.System.Scheduler.ScheduleTellRepeatedlyCancelable(interval, interval, Self, CacheCleanup.Instance, Self);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="message">TBD</param>
        /// <returns>TBD</returns>
        protected override bool Receive(object message)
        {
            if (message is Dns.Resolve r)
            {
#if DEBUG
                if (_log.IsDebugEnabled) _log.Debug("Resolution request for {0} from {1}", r.Name, Sender);
#endif
                _resolver.Forward(r);
                return true;
            }
            if (message is CacheCleanup)
            {
                if (_cacheCleanup is object)
                    _cacheCleanup.CleanUp();
                return true;
            }
            return false;
        }

        /// <summary>
        /// TBD
        /// </summary>
        protected override void PostStop()
        {
            if (_cleanupTimer is object)
                _cleanupTimer.Cancel();
        }

        /// <summary>
        /// TBD
        /// </summary>
        internal class CacheCleanup
        {
            /// <summary>
            /// TBD
            /// </summary>
            public static readonly CacheCleanup Instance = new CacheCleanup();
        }
    }
}
