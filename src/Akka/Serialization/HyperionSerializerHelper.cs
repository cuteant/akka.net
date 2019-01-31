using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Akka.Actor;
using Akka.Configuration;
using Akka.Util;
using CuteAnt.Reflection;
using Hyperion;

namespace Akka.Serialization
{
    /// <summary>HyperionSerializerHelper</summary>
    public sealed class HyperionSerializerHelper
    {
        private static Hyperion.Serializer _defaultSerializer;

        public static Hyperion.Serializer CreateSerializer(ExtendedActorSystem system, HyperionSerializerSettings settings)
        {
            return Volatile.Read(ref _defaultSerializer) ?? EnsureSerializerCreated(system, settings); ;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Hyperion.Serializer EnsureSerializerCreated(ExtendedActorSystem system, HyperionSerializerSettings settings)
        {
            var akkaSurrogate =
                Surrogate
                .Create<ISurrogated, ISurrogate>(
                from => from.ToSurrogate(system),
                to => to.FromSurrogate(system));

            var provider = CreateKnownTypesProvider(system, settings.KnownTypesProvider);

            var serializer = new Hyperion.Serializer(new SerializerOptions(
                preserveObjectReferences: settings.PreserveObjectReferences,
                versionTolerance: settings.VersionTolerance,
                surrogates: new[] { akkaSurrogate },
                knownTypes: provider.GetKnownTypes(),
                ignoreISerializable: true)
            );
            Interlocked.CompareExchange(ref _defaultSerializer, serializer, null);
            return _defaultSerializer;
        }

        private static IKnownTypesProvider CreateKnownTypesProvider(ExtendedActorSystem system, Type type)
        {
            var ctors = type.GetConstructors();
            var ctor = ctors.FirstOrDefault(c =>
            {
                var parameters = c.GetParameters();
                return parameters.Length == 1 && (parameters[0].ParameterType == typeof(ActorSystem)
                    || parameters[0].ParameterType == typeof(ExtendedActorSystem));
            });

            return ctor == null
                ? ActivatorUtils.FastCreateInstance<IKnownTypesProvider>(type)
                : (IKnownTypesProvider)ctor.Invoke(new object[] { system });
        }
    }
}