using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.Serialization.Protocol;
using CuteAnt.Reflection;

namespace Akka.Serialization
{
    #region -- SerializerWithManifest<TManifest, TSerializationManifest> --

    /// <summary>TBD</summary>
    /// <typeparam name="TManifest"></typeparam>
    public abstract class SerializerWithManifest<TManifest> : Serializer
    {
        /// <summary>Initializes a new instance of the <see cref="SerializerWithManifest{TManifest}"/> class.</summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        protected SerializerWithManifest(ExtendedActorSystem system) : base(system) { }

        /// <summary>Returns whether this serializer needs a manifest in the fromBinary method.</summary>
        public sealed override bool IncludeManifest => true;

        /// <inheritdoc />
        public override object DeepCopy(object source)
        {
            if (null == source) { return null; }

            var bts = ToBinary(source, out var manifest);
            return FromBinary(bts, manifest);
        }

        /// <inheritdoc />
        public sealed override byte[] ToBinary(object obj) => ToBinary(obj, out _);

        /// <summary>Serializes the given object into a byte array</summary>
        /// <param name="obj">The object to serialize </param>
        /// <param name="manifest">The type hint used to deserialize the object contained in the array.</param>
        /// <returns>A byte array containing the serialized object</returns>
        public abstract byte[] ToBinary(object obj, out TManifest manifest);

        /// <summary>Deserializes a byte array into an object of type <paramref name="type" />.</summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="type">The type of object contained in the array</param>
        /// <returns>The object contained in the array</returns>
        public sealed override object FromBinary(byte[] bytes, Type type)
        {
            var manifest = GetManifest(type);
            return FromBinary(bytes, manifest);
        }

        /// <summary>Deserializes a byte array into an object using an optional <paramref name="manifest"/> (type hint).</summary>
        /// <param name="bytes">The array containing the serialized object</param>
        /// <param name="manifest">The type hint used to deserialize the object contained in the array.</param>
        /// <returns>The object contained in the array</returns>
        public abstract object FromBinary(byte[] bytes, TManifest manifest);

        /// <summary>Returns the manifest (type hint) that will be provided in the <see cref="FromBinary(byte[],System.Type)"/> method.</summary>
        /// <param name="type">The type of object contained in the array</param>
        /// <returns></returns>
        protected abstract TManifest GetManifest(Type type);

        /// <summary>Returns the manifest (type hint) that will be provided in the <see cref="FromBinary(byte[],System.Type)"/> method.</summary>
        /// <param name="o">The object for which the manifest is needed.</param>
        /// <returns>The manifest needed for the deserialization of the specified <paramref name="o"/>.</returns>
        public abstract TManifest Manifest(object o);
    }

    #endregion

    #region -- SerializerWithStringManifest --

    /// <summary>TBD</summary>
    public abstract class SerializerWithStringManifest : SerializerWithManifest<string>
    {
        /// <summary>Initializes a new instance of the <see cref="SerializerWithStringManifest"/> class.</summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        protected SerializerWithStringManifest(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public sealed override Payload ToPayload(object obj)
        {
            var payload = ToBinary(obj, out var manifest);
            return new Payload(payload, Identifier, manifest, null);
        }

        /// <inheritdoc />
        public sealed override Payload ToPayloadWithAddress(Address address, object obj)
        {
            var payload = Serialization.SerializeWithTransport(system, address, this, obj, out var manifest);
            return new Payload(payload, Identifier, manifest, null);
        }

        /// <inheritdoc />
        public sealed override object FromPayload(in Payload payload)
        {
            return FromBinary(payload.Message, payload.Manifest);
        }

        /// <inheritdoc />
        public sealed override ExternalPayload ToExternalPayload(object obj)
        {
            var payload = ToBinary(obj, out var manifest);
            return new ExternalPayload(payload, Identifier, manifest, IsJson, obj.GetType());
        }

        /// <inheritdoc />
        public sealed override object FromExternalPayload(in ExternalPayload payload)
        {
            return FromBinary(payload.Message, payload.Manifest);
        }
    }

    #endregion

    #region -- SerializerWithTypeManifest --

    public abstract class SerializerWithTypeManifest : Serializer
    {
        /// <summary>Initializes a new instance of the <see cref="SerializerWithTypeManifest"/> class.</summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        protected SerializerWithTypeManifest(ExtendedActorSystem system) : base(system) { }

        /// <inheritdoc />
        public sealed override bool IncludeManifest => true;

        /// <inheritdoc />
        public sealed override Payload ToPayload(object obj)
        {
            return new Payload(ToBinary(obj), Identifier, null, obj.GetType());
        }

        /// <inheritdoc />
        public sealed override Payload ToPayloadWithAddress(Address address, object obj)
        {
            return new Payload(Serialization.SerializeWithTransport(system, address, this, obj), Identifier, null, obj.GetType());
        }

        /// <inheritdoc />
        public sealed override object FromPayload(in Payload payload)
        {
            return FromBinary(payload.Message, payload.MessageType);
        }

        /// <inheritdoc />
        public sealed override ExternalPayload ToExternalPayload(object obj)
        {
            return new ExternalPayload(ToBinary(obj), Identifier, IsJson, obj.GetType());
        }

        /// <inheritdoc />
        public sealed override object FromExternalPayload(in ExternalPayload payload)
        {
            Type type = null;
            try
            {
                type = TypeUtils.ResolveType(payload.TypeName);
            }
            catch (Exception ex)
            {
                ThrowSerializationException_D(payload.TypeName, Identifier, ex);
            }
            return FromBinary(payload.Message, type);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowSerializationException_D(string typeName, int serializerId, Exception ex)
        {
            throw GetSerializationException();

            SerializationException GetSerializationException()
            {
                return new SerializationException($"Cannot find manifest class [{typeName}] for serializer with id [{serializerId}].", ex);
            }
        }
    }

    #endregion
}
