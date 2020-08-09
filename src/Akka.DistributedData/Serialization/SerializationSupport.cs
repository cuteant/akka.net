using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Akka.Actor;
using Akka.Serialization;
using Address = Akka.Actor.Address;
using OtherMessage = Akka.Serialization.Protocol.Payload;
using UniqueAddress = Akka.Cluster.UniqueAddress;

namespace Akka.DistributedData.Serialization
{
    /// <summary>
    /// INTERNAL API.
    ///
    /// Used to support the DData serializers.
    /// </summary>
    internal sealed class SerializationSupport
    {
        public SerializationSupport(ExtendedActorSystem system)
        {
            System = system;
        }

        public ExtendedActorSystem System { get; }

        private volatile Akka.Serialization.Serialization _ser;

        public Akka.Serialization.Serialization Serialization
        {
            [MethodImpl(InlineOptions.AggressiveOptimization)]
            get => _ser ?? EnsureSerializationCreated();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Akka.Serialization.Serialization EnsureSerializationCreated()
        {
            lock (this)
            {
                if (_ser == null)
                {
                    _ser = new Akka.Serialization.Serialization(System);
                }
            }
            return _ser;
        }

        private volatile string _protocol;

        public string AddressProtocol
        {
            get
            {
                if (_protocol == null)
                    _protocol = System.Provider.DefaultAddress.Protocol;
                return _protocol;
            }
        }

        private volatile Information _transportInfo;

        public Information TransportInfo
        {
            get
            {
                if (_transportInfo == null)
                {
                    var address = System.Provider.DefaultAddress;
                    _transportInfo = new Information(address, System);
                }

                return _transportInfo;
            }
        }

        public static Protocol.Address AddressToProto(Address address)
        {
            if (string.IsNullOrEmpty(address.Host) || !address.Port.HasValue)
            {
                ThrowAddressCouldNotBeSerialized(address);
            }

            return new Protocol.Address(address.Host, address.Port.Value);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowAddressCouldNotBeSerialized(Address address)
        {
            throw GetException();
            ArgumentOutOfRangeException GetException()
            {
                throw new ArgumentOutOfRangeException(
                    $"Address [{address}] could not be serialized: host or port missing.");
            }
        }

        public Address AddressFromProto(in Protocol.Address address)
        {
            return new Address(AddressProtocol, System.Name, address.HostName, address.Port);
        }

        public static Protocol.UniqueAddress UniqueAddressToProto(UniqueAddress address)
        {
            return new Protocol.UniqueAddress(AddressToProto(address.Address), address.Uid);
        }

        public UniqueAddress UniqueAddressFromProto(in Protocol.UniqueAddress address)
        {
            return new UniqueAddress(AddressFromProto(address.Address), (int)address.Uid);
        }

        public static Protocol.VersionVector VersionVectorToProto(VersionVector versionVector)
        {
            var entries = new List<Protocol.VersionVector.Entry>();

            using (var enumerator = versionVector.VersionEnumerator)
            {
                while (enumerator.MoveNext())
                {
                    var current = enumerator.Current;
                    entries.Add(new Protocol.VersionVector.Entry(UniqueAddressToProto(current.Key), current.Value));
                }
            }

            return new Protocol.VersionVector(entries);
        }

        public VersionVector VersionVectorFromProto(in Protocol.VersionVector versionVector)
        {
            var entries = versionVector.Entries;
            if (0u >= (uint)entries.Count)
            {
                return VersionVector.Empty;
            }
            if (0u >= (uint)(entries.Count - 1))
            {
                return new SingleVersionVector(UniqueAddressFromProto(versionVector.Entries[0].Node),
                    versionVector.Entries[0].Version);
            }
            var versions = entries.ToDictionary(x => UniqueAddressFromProto(x.Node), v => v.Version);
            return new MultiVersionVector(versions);
        }

        //public VersionVector VersionVectorFromBinary(byte[] bytes)
        //{
        //    return VersionVectorFromProto(Protocol.VersionVector.Parser.ParseFrom(bytes));
        //}

        public IActorRef ResolveActorRef(string path)
        {
            return System.Provider.ResolveActorRef(path);
        }

        public OtherMessage OtherMessageToProto(object msg)
        {
            return Serialization.SerializeMessageWithTransport(msg);
        }

        //public object OtherMessageFromBytes(byte[] other)
        //{
        //    return OtherMessageFromProto(OtherMessage.Parser.ParseFrom(other));
        //}

        public object OtherMessageFromProto(in OtherMessage other)
        {
            return Serialization.Deserialize(other);
        }
    }
}
