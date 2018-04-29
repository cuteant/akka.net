//-----------------------------------------------------------------------
// <copyright file="EndpointRegistry.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Akka.Util.Internal;

namespace Akka.Remote
{
    /// <summary>
    /// Not threadsafe - only to be used in HeadActor
    /// </summary>
    internal class EndpointRegistry
    {
        private readonly Dictionary<Address, Tuple<IActorRef, int>> _addressToReadonly = new Dictionary<Address, Tuple<IActorRef, int>>();

        private Dictionary<Address, EndpointManager.EndpointPolicy> _addressToWritable = new Dictionary<Address, EndpointManager.EndpointPolicy>();

        private readonly Dictionary<IActorRef, Address> _readonlyToAddress = new Dictionary<IActorRef, Address>();
        private readonly Dictionary<IActorRef, Address> _writableToAddress = new Dictionary<IActorRef, Address>();

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="address">TBD</param>
        /// <param name="endpoint">TBD</param>
        /// <param name="uid">TBD</param>
        /// <param name="refuseUid">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="address"/> does not have
        /// an <see cref="EndpointManager.EndpointPolicy"/> of <see cref="EndpointManager.Pass"/>
        /// in the registry.
        /// </exception>
        /// <returns>TBD</returns>
        public IActorRef RegisterWritableEndpoint(Address address, IActorRef endpoint, int? uid, int? refuseUid)
        {
            _addressToWritable.TryGetValue(address, out var existing);

            if (existing is EndpointManager.Pass pass) // if we already have a writable endpoint....
            {
                throw new ArgumentException($"Attempting to overwrite existing endpoint {pass.Endpoint} with {endpoint}");
            }

            _addressToWritable[address] = new EndpointManager.Pass(endpoint, uid, refuseUid);
            _writableToAddress[endpoint] = address;
            return endpoint;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="uid">TBD</param>
        public void RegisterWritableEndpointUid(Address remoteAddress, int uid)
        {
            if (_addressToWritable.TryGetValue(remoteAddress, out var existing))
            {
                if (existing is EndpointManager.Pass pass)
                {
                    _addressToWritable[remoteAddress] = new EndpointManager.Pass(pass.Endpoint, uid, pass.RefuseUid);
                }
                // if the policy is not Pass, then the GotUid might have lost the race with some failure
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="remoteAddress">TBD</param>
        /// <param name="refuseUid">TBD</param>
        public void RegisterWritableEndpointRefuseUid(Address remoteAddress, int refuseUid)
        {
            if (_addressToWritable.TryGetValue(remoteAddress, out var existing))
            {
                switch (existing)
                {
                    case EndpointManager.Pass pass:
                        _addressToWritable[remoteAddress] = new EndpointManager.Pass(pass.Endpoint, pass.Uid, refuseUid);
                        break;
                    case EndpointManager.Gated gated:
                        _addressToWritable[remoteAddress] = new EndpointManager.Gated(gated.TimeOfRelease, refuseUid);
                        break;
                    case EndpointManager.WasGated wasGated:
                        _addressToWritable[remoteAddress] = new EndpointManager.WasGated(refuseUid);
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="address">TBD</param>
        /// <param name="endpoint">TBD</param>
        /// <param name="uid">TBD</param>
        /// <returns>TBD</returns>
        public IActorRef RegisterReadOnlyEndpoint(Address address, IActorRef endpoint, int uid)
        {
            _addressToReadonly[address] = Tuple.Create(endpoint, uid);
            _readonlyToAddress[endpoint] = address;
            return endpoint;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="endpoint">TBD</param>
        public void UnregisterEndpoint(IActorRef endpoint)
        {
            if (IsWritable(endpoint))
            {
                var address = _writableToAddress[endpoint];
                _addressToWritable.TryGetValue(address, out var policy);
                if (policy != null && policy.IsTombstone)
                {
                    //if there is already a tombstone directive, leave it there
                }
                else
                {
                    _addressToWritable.Remove(address);
                }

                _writableToAddress.Remove(endpoint);
            }
            else if (IsReadOnly(endpoint))
            {
                _addressToReadonly.Remove(_readonlyToAddress[endpoint]);
                _readonlyToAddress.Remove(endpoint);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="writer">TBD</param>
        /// <returns>TBD</returns>
        public Address AddressForWriter(IActorRef writer)
        {
            // Needs to return null if the key is not in the dictionary, instead of throwing.
            _writableToAddress.TryGetValue(writer, out var value);
            return value;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="address">TBD</param>
        /// <returns>TBD</returns>
        public Tuple<IActorRef, int> ReadOnlyEndpointFor(Address address)
        {
            _addressToReadonly.TryGetValue(address, out var tmp);
            return tmp;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="endpoint">TBD</param>
        /// <returns>TBD</returns>
        public bool IsWritable(IActorRef endpoint)
        {
            return _writableToAddress.ContainsKey(endpoint);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="endpoint">TBD</param>
        /// <returns>TBD</returns>
        public bool IsReadOnly(IActorRef endpoint)
        {
            return _readonlyToAddress.ContainsKey(endpoint);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="address">TBD</param>
        /// <param name="uid">TBD</param>
        /// <returns>TBD</returns>
        public bool IsQuarantined(Address address, int uid)
        {
            // timeOfRelease is only used for garbage collection. If an address is still probed, we should report the
            // known fact that it is quarantined.
            var policy = WritableEndpointWithPolicyFor(address) as EndpointManager.Quarantined;
            return policy?.Uid == uid;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="address">TBD</param>
        /// <returns>TBD</returns>
        public int? RefuseUid(Address address)
        {
            // timeOfRelease is only used for garbage collection. If an address is still probed, we should report the
            // known fact that it is quarantined.
            var policy = WritableEndpointWithPolicyFor(address);
            switch (policy)
            {
                case EndpointManager.Quarantined q:
                    return q.Uid;
                case EndpointManager.Pass p:
                    return p.RefuseUid;
                case EndpointManager.Gated g:
                    return g.RefuseUid;
                case EndpointManager.WasGated w:
                    return w.RefuseUid;
                default:
                    return null;
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="address">TBD</param>
        /// <returns>TBD</returns>
        public EndpointManager.EndpointPolicy WritableEndpointWithPolicyFor(Address address)
        {
            _addressToWritable.TryGetValue(address, out var tmp);
            return tmp;
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="address">TBD</param>
        /// <returns>TBD</returns>
        public bool HasWriteableEndpointFor(Address address)
        {
            var policy = WritableEndpointWithPolicyFor(address);
            return policy is EndpointManager.Pass || policy is EndpointManager.WasGated;
        }

        /// <summary>
        /// Marking an endpoint as failed means that we will not try to connect to the remote system within
        /// the gated period but it is ok for the remote system to try to connect with us (inbound-only.)
        /// </summary>
        /// <param name="endpoint">TBD</param>
        /// <param name="timeOfRelease">TBD</param>
        public void MarkAsFailed(IActorRef endpoint, Deadline timeOfRelease)
        {
            if (IsWritable(endpoint))
            {
                var address = _writableToAddress[endpoint];
                if (_addressToWritable.TryGetValue(address, out var policy))
                {
                    switch (policy)
                    {
                        case EndpointManager.Quarantined _:
                            // don't overwrite Quarantined with Gated
                            break;
                        case EndpointManager.Pass _:
                            _addressToWritable[address] = new EndpointManager.Gated(timeOfRelease,
                                policy.AsInstanceOf<EndpointManager.Pass>().RefuseUid);
                            _writableToAddress.Remove(endpoint);
                            break;
                        case EndpointManager.WasGated _:
                            _addressToWritable[address] = new EndpointManager.Gated(timeOfRelease,
                                policy.AsInstanceOf<EndpointManager.WasGated>().RefuseUid);
                            _writableToAddress.Remove(endpoint);
                            break;
                        case EndpointManager.Gated _:
                            // already gated
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    _addressToWritable[address] = new EndpointManager.Gated(timeOfRelease, null);
                    _writableToAddress.Remove(endpoint);
                }
            }
            else if (IsReadOnly(endpoint))
            {
                _addressToReadonly.Remove(_readonlyToAddress[endpoint]);
                _readonlyToAddress.Remove(endpoint);
            }
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="address">TBD</param>
        /// <param name="uid">TBD</param>
        /// <param name="timeOfRelease">TBD</param>
        public void MarkAsQuarantined(Address address, int uid, Deadline timeOfRelease)
        {
            _addressToWritable[address] = new EndpointManager.Quarantined(uid, timeOfRelease);
        }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="address">TBD</param>
        public void RemovePolicy(Address address) => _addressToWritable.Remove(address);

        /// <summary>
        /// TBD
        /// </summary>
        public IList<IActorRef> AllEndpoints => _writableToAddress.Keys.Concat(_readonlyToAddress.Keys).ToList();

        /// <summary>
        /// TBD
        /// </summary>
        public void Prune()
        {
            _addressToWritable = _addressToWritable.Where(x =>
            !(x.Value is EndpointManager.Quarantined
            && !((EndpointManager.Quarantined)x.Value).Deadline.HasTimeLeft)).Select(entry =>
            {
                var key = entry.Key;
                var policy = entry.Value;
                if (policy is EndpointManager.Gated gated)
                {
                    if (gated.TimeOfRelease.HasTimeLeft) { return entry; }
                    return new KeyValuePair<Address, EndpointManager.EndpointPolicy>(key, new EndpointManager.WasGated(gated.RefuseUid));
                }
                return entry;
            }).ToDictionary(key => key.Key, value => value.Value);
        }

        /// <summary>
        /// Internal function used for filtering endpoints that need to be pruned due to non-recovery past their deadlines
        /// </summary>
        private static bool PruneFilterFunction(EndpointManager.EndpointPolicy policy)
        {
            var rValue = true;

            policy.Match()
                .With<EndpointManager.Gated>(g => rValue = g.TimeOfRelease.HasTimeLeft)
                .With<EndpointManager.Quarantined>(q => rValue = q.Deadline.HasTimeLeft)
                .Default(msg => rValue = true);

            return rValue;
        }
    }
}

