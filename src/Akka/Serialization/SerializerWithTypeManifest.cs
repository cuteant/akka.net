using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using Akka.Actor;
using Akka.Serialization.Protocol;
using CuteAnt.Reflection;

namespace Akka.Serialization
{
    public abstract class SerializerWithTypeManifest : Serializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SerializerWithTypeManifest"/> class.
        /// </summary>
        /// <param name="system">The actor system to associate with this serializer.</param>
        protected SerializerWithTypeManifest(ExtendedActorSystem system) : base(system)
        {
        }

        /// <inheritdoc />
        public sealed override bool IncludeManifest => true;

        /// <inheritdoc />
        public sealed override Payload ToPayload(object obj)
        {
            var typeKey = TypeSerializer.GetTypeKeyFromType(obj.GetType());
            return new Payload(ToBinary(obj), Identifier, typeKey.TypeName, typeKey.HashCode);
        }

        /// <inheritdoc />
        public sealed override Payload ToPayloadWithAddress(Address address, object obj)
        {
            var typeKey = TypeSerializer.GetTypeKeyFromType(obj.GetType());
            return new Payload(ToBinaryWithAddress(address, obj), Identifier, typeKey.TypeName, typeKey.HashCode);
        }

        /// <inheritdoc />
        public sealed override object FromPayload(in Payload payload)
        {
            var typeKey = new TypeKey(payload.TypeHashCode, payload.MessageManifest);
            Type type = null;
            try
            {
                type = TypeSerializer.GetTypeFromTypeKey(typeKey);
            }
            catch (Exception ex)
            {
                ThrowSerializationException_D(typeKey, Identifier, ex);
            }
            return FromBinary(payload.Message, type);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowSerializationException_D(in TypeKey typeKey, int serializerId, Exception ex)
        {
            var typeName = Encoding.UTF8.GetString(typeKey.TypeName);
            throw GetSerializationException();

            SerializationException GetSerializationException()
            {
                return new SerializationException($"Cannot find manifest class [{typeName}] for serializer with id [{serializerId}].", ex);
            }
        }
    }
}
