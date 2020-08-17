//-----------------------------------------------------------------------
// <copyright file="ReplicatedDataSerializer.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2019 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2019 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Akka.Actor;
using Akka.DistributedData.Internal;
using Akka.Serialization;
using Akka.Util.Internal;
using CuteAnt.Reflection;
using MessagePack;
using ArgumentOutOfRangeException = System.ArgumentOutOfRangeException;
using IActorRef = Akka.Actor.IActorRef;

namespace Akka.DistributedData.Serialization
{
    public sealed class ReplicatedDataSerializer : SerializerWithStringManifest
    {
        #region manifests

        private const string DeletedDataManifest = "A";
        private const string GSetManifest = "B";
        private const string GSetKeyManifest = "b";
        private const string ORSetManifest = "C";
        private const string ORSetKeyManifest = "c";
        private const string ORSetAddManifest = "Ca";
        private const string ORSetRemoveManifest = "Cr";
        private const string ORSetFullManifest = "Cf";
        private const string ORSetDeltaGroupManifest = "Cg";
        private const string FlagManifest = "D";
        private const string FlagKeyManifest = "d";
        private const string LWWRegisterManifest = "E";
        private const string LWWRegisterKeyManifest = "e";
        private const string GCounterManifest = "F";
        private const string GCounterKeyManifest = "f";
        private const string PNCounterManifest = "G";
        private const string PNCounterKeyManifest = "g";
        private const string ORMapManifest = "H";
        private const string ORMapKeyManifest = "h";
        private const string ORMapPutManifest = "Ha";
        private const string ORMapRemoveManifest = "Hr";
        private const string ORMapRemoveKeyManifest = "Hk";
        private const string ORMapUpdateManifest = "Hu";
        private const string ORMapDeltaGroupManifest = "Hg";
        private const string LWWMapManifest = "I";
        private const string LWWMapDeltaGroupManifest = "Ig";
        private const string LWWMapKeyManifest = "i";
        private const string PNCounterMapManifest = "J";
        private const string PNCounterMapDeltaOperationManifest = "Jo";
        private const string PNCounterMapKeyManifest = "j";
        private const string ORMultiMapManifest = "K";
        private const string ORMultiMapDeltaOperationManifest = "Ko";
        private const string ORMultiMapKeyManifest = "k";
        private const string VersionVectorManifest = "L";

        private static readonly IFormatterResolver s_defaultResolver;
        private static readonly Dictionary<Type, Protocol.TypeDescriptor> s_typeDescriptorMap;
        private static readonly Dictionary<Type, string> s_manifestMap;

        static ReplicatedDataSerializer()
        {
            s_defaultResolver = MessagePackSerializer.DefaultResolver;
            s_typeDescriptorMap = new Dictionary<Type, Protocol.TypeDescriptor>()
            {
                { typeof(string), new Protocol.TypeDescriptor(Protocol.ValType.String) },
                { typeof(int), new Protocol.TypeDescriptor(Protocol.ValType.Int) },
                { typeof(long), new Protocol.TypeDescriptor(Protocol.ValType.Long) },
                { typeof(IActorRef), new Protocol.TypeDescriptor(Protocol.ValType.ActorRef) },
            };
            s_manifestMap = new Dictionary<Type, string>()
            {
                { typeof(IORSet), ORSetManifest },
                { typeof(ORSet.IAddDeltaOperation), ORSetAddManifest },
                { typeof(ORSet.IRemoveDeltaOperation), ORSetRemoveManifest },
                { typeof(IGSet), GSetManifest },
                { typeof(GCounter), GCounterManifest },
                { typeof(PNCounter), PNCounterManifest },
                { typeof(Flag), FlagManifest },
                { typeof(ILWWRegister), LWWRegisterManifest },
                { typeof(IORDictionary), ORMapManifest },
                { typeof(ORDictionary.IPutDeltaOp), ORMapPutManifest },
                { typeof(ORDictionary.IRemoveDeltaOp), ORMapRemoveManifest },
                { typeof(ORDictionary.IRemoveKeyDeltaOp), ORMapRemoveKeyManifest },
                { typeof(ORDictionary.IUpdateDeltaOp), ORMapUpdateManifest },
                { typeof(ILWWDictionary), LWWMapManifest },
                { typeof(ILWWDictionaryDeltaOperation), LWWMapDeltaGroupManifest },
                { typeof(IPNCounterDictionary), PNCounterMapManifest },
                { typeof(IPNCounterDictionaryDeltaOperation), PNCounterMapDeltaOperationManifest },
                { typeof(IORMultiValueDictionary), ORMultiMapManifest },
                { typeof(IORMultiValueDictionaryDeltaOperation), ORMultiMapDeltaOperationManifest },
                { typeof(DeletedData), DeletedDataManifest },
                { typeof(VersionVector), VersionVectorManifest },

                // key types
                { typeof(IORSetKey), ORSetKeyManifest },
                { typeof(IGSetKey), GSetKeyManifest },
                { typeof(GCounterKey), GCounterKeyManifest },
                { typeof(PNCounterKey), PNCounterKeyManifest },
                { typeof(FlagKey), FlagKeyManifest },
                { typeof(ILWWRegisterKey), LWWRegisterKeyManifest },
                { typeof(IORDictionaryKey), ORMapKeyManifest },
                { typeof(ILWWDictionaryKey), LWWMapKeyManifest },
                { typeof(IPNCounterDictionaryKey), PNCounterMapKeyManifest },
                { typeof(IORMultiValueDictionaryKey), ORMultiMapKeyManifest },

                // less common delta types
                { typeof(ORSet.IDeltaGroupOperation), ORSetDeltaGroupManifest },
                { typeof(ORDictionary.IDeltaGroupOp), ORMapDeltaGroupManifest },
                { typeof(ORSet.IFullStateDeltaOperation), ORSetFullManifest },
            };
        }

        #endregion

        private readonly SerializationSupport _ser;

        private readonly byte[] _emptyArray = Array.Empty<byte>();

        public ReplicatedDataSerializer(ExtendedActorSystem system) : base(system)
        {
            _ser = new SerializationSupport(system);
        }

        public override byte[] ToBinary(object obj, out string manifest)
        {
            switch (obj)
            {
                case IORSet o:
                    manifest = ORSetManifest;
                    return LZ4MessagePackSerializer.Serialize(ToProto(o), s_defaultResolver);
                case ORSet.IAddDeltaOperation o:
                    manifest = ORSetAddManifest;
                    return MessagePackSerializer.Serialize(ToProto(o.UnderlyingSerialization), s_defaultResolver);
                case ORSet.IRemoveDeltaOperation o:
                    manifest = ORSetRemoveManifest;
                    return MessagePackSerializer.Serialize(ToProto(o.UnderlyingSerialization), s_defaultResolver);
                case IGSet g:
                    manifest = GSetManifest;
                    return MessagePackSerializer.Serialize(ToProto(g), s_defaultResolver);
                case GCounter g:
                    manifest = GCounterManifest;
                    return MessagePackSerializer.Serialize(ToProto(g), s_defaultResolver);
                case PNCounter p:
                    manifest = PNCounterManifest;
                    return MessagePackSerializer.Serialize(ToProto(p), s_defaultResolver);
                case Flag f:
                    manifest = FlagManifest;
                    return MessagePackSerializer.Serialize(ToProto(f), s_defaultResolver);
                case ILWWRegister l:
                    manifest = LWWRegisterManifest;
                    return MessagePackSerializer.Serialize(ToProto(l), s_defaultResolver);
                case IORDictionary o:
                    manifest = ORMapManifest;
                    return LZ4MessagePackSerializer.Serialize(ToProto(o), s_defaultResolver);
                case ORDictionary.IPutDeltaOp p:
                    manifest = ORMapPutManifest;
                    return MessagePackSerializer.Serialize(ORDictionaryPutToProto(p), s_defaultResolver);
                case ORDictionary.IRemoveDeltaOp r:
                    manifest = ORMapRemoveManifest;
                    return MessagePackSerializer.Serialize(ORDictionaryRemoveToProto(r), s_defaultResolver);
                case ORDictionary.IRemoveKeyDeltaOp r:
                    manifest = ORMapRemoveKeyManifest;
                    return MessagePackSerializer.Serialize(ORDictionaryRemoveKeyToProto(r), s_defaultResolver);
                case ORDictionary.IUpdateDeltaOp u:
                    manifest = ORMapUpdateManifest;
                    return MessagePackSerializer.Serialize(ORDictionaryUpdateToProto(u), s_defaultResolver);
                case ILWWDictionary l:
                    manifest = LWWMapManifest;
                    return LZ4MessagePackSerializer.Serialize(ToProto(l), s_defaultResolver);
                case ILWWDictionaryDeltaOperation ld:
                    manifest = LWWMapDeltaGroupManifest;
                    return MessagePackSerializer.Serialize(ToProto(ld.Underlying), s_defaultResolver);
                case IPNCounterDictionary pn:
                    manifest = PNCounterMapManifest;
                    return LZ4MessagePackSerializer.Serialize(ToProto(pn), s_defaultResolver);
                case IPNCounterDictionaryDeltaOperation pnd:
                    manifest = PNCounterMapDeltaOperationManifest;
                    return MessagePackSerializer.Serialize(ToProto(pnd.Underlying), s_defaultResolver);
                case IORMultiValueDictionary m:
                    manifest = ORMultiMapManifest;
                    return LZ4MessagePackSerializer.Serialize(ToProto(m), s_defaultResolver);
                case IORMultiValueDictionaryDeltaOperation md:
                    manifest = ORMultiMapDeltaOperationManifest;
                    return MessagePackSerializer.Serialize(ToProto(md), s_defaultResolver);
                case DeletedData _:
                    manifest = DeletedDataManifest;
                    return _emptyArray;
                case VersionVector v:
                    manifest = VersionVectorManifest;
                    return MessagePackSerializer.Serialize(SerializationSupport.VersionVectorToProto(v), s_defaultResolver);

                // key types
                case IKey k:
                    return MessagePackSerializer.Serialize(ToProto(k, out manifest), s_defaultResolver);

                // less common delta types
                case ORSet.IDeltaGroupOperation o:
                    manifest = ORSetDeltaGroupManifest;
                    return MessagePackSerializer.Serialize(ToProto(o), s_defaultResolver);
                case ORDictionary.IDeltaGroupOp g:
                    manifest = ORMapDeltaGroupManifest;
                    return MessagePackSerializer.Serialize(ORDictionaryDeltasToProto(g.OperationsSerialization.ToList()), s_defaultResolver);
                case ORSet.IFullStateDeltaOperation o:
                    manifest = ORSetFullManifest;
                    return MessagePackSerializer.Serialize(ToProto(o.UnderlyingSerialization), s_defaultResolver);

                default: throw GetSerializeException(obj);
            }
        }

        public override object FromBinary(byte[] bytes, string manifest)
        {
            switch (manifest)
            {
                case ORSetManifest: return ORSetFromLZ4Binary(bytes);
                case ORSetAddManifest: return ORAddDeltaOperationFromBinary(bytes);
                case ORSetRemoveManifest: return ORRemoveOperationFromBinary(bytes);
                case GSetManifest: return GSetFromBinary(bytes);
                case GCounterManifest: return GCounterFromBytes(bytes);
                case PNCounterManifest: return PNCounterFromBytes(bytes);
                case FlagManifest: return FlagFromBinary(bytes);
                case LWWRegisterManifest: return LWWRegisterFromBinary(bytes);
                case ORMapManifest: return ORDictionaryFromLZ4Binary(bytes);
                case ORMapPutManifest: return ORDictionaryPutFromBinary(bytes);
                case ORMapRemoveManifest: return ORDictionaryRemoveFromBinary(bytes);
                case ORMapRemoveKeyManifest: return ORDictionaryRemoveKeyFromBinary(bytes);
                case ORMapUpdateManifest: return ORDictionaryUpdateFromBinary(bytes);
                case ORMapDeltaGroupManifest: return ORDictionaryDeltaGroupFromBinary(bytes);
                case LWWMapManifest: return LWWDictionaryFromLZ4Binary(bytes);
                case LWWMapDeltaGroupManifest: return LWWDictionaryDeltaGroupFromBinary(bytes);
                case PNCounterMapManifest: return PNCounterDictionaryFromLZ4Binary(bytes);
                case PNCounterMapDeltaOperationManifest: return PNCounterDeltaFromBinary(bytes);
                case ORMultiMapManifest: return ORMultiDictionaryFromLZ4Binary(bytes);
                case ORMultiMapDeltaOperationManifest: return ORMultiDictionaryDeltaFromBinary(bytes);
                case DeletedDataManifest: return DeletedData.Instance;
                case VersionVectorManifest: return /*_ser.*/VersionVectorFromBinary(bytes);

                // key types
                case ORSetKeyManifest: return ORSetKeyFromBinary(bytes);
                case GSetKeyManifest: return GSetKeyFromBinary(bytes);
                case GCounterKeyManifest: return GCounterKeyFromBinary(bytes);
                case PNCounterKeyManifest: return PNCounterKeyFromBinary(bytes);
                case FlagKeyManifest: return FlagKeyFromBinary(bytes);
                case LWWRegisterKeyManifest: return LWWRegisterKeyFromBinary(bytes);
                case ORMapKeyManifest: return ORDictionaryKeyFromBinary(bytes);
                case LWWMapKeyManifest: return LWWDictionaryKeyFromBinary(bytes);
                case PNCounterMapKeyManifest: return PNCounterDictionaryKeyFromBinary(bytes);
                case ORMultiMapKeyManifest: return ORMultiValueDictionaryKeyFromBinary(bytes);

                // less common delta types
                case ORSetDeltaGroupManifest: return ORDeltaGroupOperationFromBinary(bytes);
                case ORSetFullManifest: return ORFullStateDeltaOperationFromBinary(bytes);

                default: throw GetDeserializationException(manifest);
            }
        }

        /// <inheritdoc />
        protected override string GetManifest(Type type)
        {
            if (type is null) { return null; }
            var manifestMap = s_manifestMap;
            if (manifestMap.TryGetValue(type, out var manifest)) { return manifest; }
            foreach (var item in manifestMap)
            {
                if (item.Key.IsAssignableFrom(type)) { return item.Value; }
            }
            throw GetSerializeException(type);
        }

        public override string Manifest(object o)
        {
            switch (o)
            {
                case IORSet _: return ORSetManifest;
                case ORSet.IAddDeltaOperation _: return ORSetAddManifest;
                case ORSet.IRemoveDeltaOperation _: return ORSetRemoveManifest;
                case IGSet _: return GSetManifest;
                case GCounter _: return GCounterManifest;
                case PNCounter _: return PNCounterManifest;
                case Flag _: return FlagManifest;
                case ILWWRegister _: return LWWRegisterManifest;
                case IORDictionary _: return ORMapManifest;
                case ORDictionary.IPutDeltaOp _: return ORMapPutManifest;
                case ORDictionary.IRemoveDeltaOp _: return ORMapRemoveManifest;
                case ORDictionary.IRemoveKeyDeltaOp _: return ORMapRemoveKeyManifest;
                case ORDictionary.IUpdateDeltaOp _: return ORMapUpdateManifest;
                case ILWWDictionary _: return LWWMapManifest;
                case ILWWDictionaryDeltaOperation _: return LWWMapDeltaGroupManifest;
                case IPNCounterDictionary _: return PNCounterMapManifest;
                case IPNCounterDictionaryDeltaOperation _: return PNCounterMapDeltaOperationManifest;
                case IORMultiValueDictionary _: return ORMultiMapManifest;
                case IORMultiValueDictionaryDeltaOperation _: return ORMultiMapDeltaOperationManifest;
                case DeletedData _: return DeletedDataManifest;
                case VersionVector _: return VersionVectorManifest;

                // key types
                case IORSetKey _: return ORSetKeyManifest;
                case IGSetKey _: return GSetKeyManifest;
                case GCounterKey _: return GCounterKeyManifest;
                case PNCounterKey _: return PNCounterKeyManifest;
                case FlagKey _: return FlagKeyManifest;
                case ILWWRegisterKey _: return LWWRegisterKeyManifest;
                case IORDictionaryKey _: return ORMapKeyManifest;
                case ILWWDictionaryKey _: return LWWMapKeyManifest;
                case IPNCounterDictionaryKey _: return PNCounterMapKeyManifest;
                case IORMultiValueDictionaryKey _: return ORMultiMapKeyManifest;

                // less common delta types
                case ORSet.IDeltaGroupOperation _: return ORSetDeltaGroupManifest;
                case ORDictionary.IDeltaGroupOp _: return ORMapDeltaGroupManifest;
                case ORSet.IFullStateDeltaOperation _: return ORSetFullManifest;

                default: throw GetSerializeException(o);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentException GetSerializeException(object o)
        {
            var type = o is Type t ? t : o.GetType();
            return new ArgumentException($"Can't serialize object of type [{type.FullName}] in [{nameof(ReplicatedDataSerializer)}]");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentException GetDeserializationException(string manifest)
        {
            return new ArgumentException($"Can't deserialize object with unknown manifest [{manifest}]");
        }

        private VersionVector VersionVectorFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.VersionVector>(bytes, s_defaultResolver);
            return _ser.VersionVectorFromProto(proto);
        }

        #region TypeDescriptor

        private static Protocol.TypeDescriptor GetTypeDescriptor(Type t)
        {
            if (s_typeDescriptorMap.TryGetValue(t, out var typeInfo)) { return typeInfo; }

            return new Protocol.TypeDescriptor(Protocol.ValType.Other, RuntimeTypeNameFormatter.Serialize(t));
        }

        private static Type GetTypeFromDescriptor(in Protocol.TypeDescriptor t)
        {
            switch (t.Type)
            {
                case Protocol.ValType.Int:
                    return typeof(int);
                case Protocol.ValType.Long:
                    return typeof(long);
                case Protocol.ValType.String:
                    return typeof(string);
                case Protocol.ValType.ActorRef:
                    return typeof(IActorRef);
                case Protocol.ValType.Other:
                    {
                        var type = TypeUtils.ResolveType(t.TypeName);
                        return type;
                    }
                default:
                    throw GetUnKnownValTypeException(t);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SerializationException GetUnKnownValTypeException(in Protocol.TypeDescriptor t)
        {
            return new SerializationException($"Unknown ValType of [{t.Type}] detected");
        }

        #endregion

        #region ORSet

        private static Protocol.ORSet ORSetToProto<T>(ORSet<T> set)
        {
            return new Protocol.ORSet
            {
                Vvector = SerializationSupport.VersionVectorToProto(set.VersionVector),
                Dots = set.ElementsMap.Values.Select(SerializationSupport.VersionVectorToProto).ToList()
            };
        }
        private IORSet ORSetFromLZ4Binary(in ReadOnlySpan<byte> bytes)
        {
            return FromProto(LZ4MessagePackSerializer.Deserialize<Protocol.ORSet>(bytes, s_defaultResolver));
        }

        private Protocol.ORSet ToProto(IORSet orset)
        {
            switch (orset)
            {
                case ORSet<int> ints:
                    {
                        var p = ORSetToProto(ints);
                        p.TypeInfo = new Protocol.TypeDescriptor(Protocol.ValType.Int);
                        p.IntElements = ints.Elements.ToList();
                        return p;
                    }
                case ORSet<long> longs:
                    {
                        var p = ORSetToProto(longs);
                        p.TypeInfo = new Protocol.TypeDescriptor(Protocol.ValType.Long);
                        p.LongElements = longs.Elements.ToList();
                        return p;
                    }
                case ORSet<string> strings:
                    {
                        var p = ORSetToProto(strings);
                        p.TypeInfo = new Protocol.TypeDescriptor(Protocol.ValType.String);
                        p.StringElements = strings.Elements.ToList();
                        return p;
                    }
                case ORSet<IActorRef> refs:
                    {
                        var p = ORSetToProto(refs);
                        p.TypeInfo = new Protocol.TypeDescriptor(Protocol.ValType.ActorRef);
                        p.ActorRefElements = refs.Select(Akka.Serialization.Serialization.SerializedActorPath).ToList();
                        return p;
                    }
                default: // unknown type
                    {
                        // runtime type - enter horrible dynamic serialization stuff
                        var makeProto = ORSetUnknownMaker.MakeGenericMethod(orset.SetType);
                        return (Protocol.ORSet)makeProto.Invoke(this, new object[] { orset });
                    }
            }
        }

        private IORSet FromProto(Protocol.ORSet orset)
        {
            var dots = orset.Dots.Select(x => _ser.VersionVectorFromProto(x));
            var vector = _ser.VersionVectorFromProto(orset.Vvector);

            var intElements = orset.IntElements;
            if ((intElements is object && (uint)intElements.Count > 0u) || orset.TypeInfo.Type == Protocol.ValType.Int)
            {
                var eInt = intElements.Zip(dots, (i, versionVector) => (i, versionVector))
                    .ToImmutableDictionary(x => x.i, y => y.versionVector);

                return new ORSet<int>(eInt, vector);
            }

            var longElements = orset.LongElements;
            if ((longElements is object && (uint)longElements.Count > 0u) || orset.TypeInfo.Type == Protocol.ValType.Long)
            {
                var eLong = longElements.Zip(dots, (i, versionVector) => (i, versionVector))
                    .ToImmutableDictionary(x => x.i, y => y.versionVector);
                return new ORSet<long>(eLong, vector);
            }

            var stringElements = orset.StringElements;
            if ((stringElements is object && (uint)stringElements.Count > 0u) || orset.TypeInfo.Type == Protocol.ValType.String)
            {
                var eStr = stringElements.Zip(dots, (i, versionVector) => (i, versionVector))
                    .ToImmutableDictionary(x => x.i, y => y.versionVector);
                return new ORSet<string>(eStr, vector);
            }

            var actorRefElements = orset.ActorRefElements;
            if ((actorRefElements is object && (uint)actorRefElements.Count > 0u) || orset.TypeInfo.Type == Protocol.ValType.ActorRef)
            {
                var eRef = actorRefElements.Zip(dots, (i, versionVector) => (i, versionVector))
                    .ToImmutableDictionary(x => _ser.ResolveActorRef(x.i), y => y.versionVector);
                return new ORSet<IActorRef>(eRef, vector);
            }

            // runtime type - enter horrible dynamic serialization stuff

            var setContentType = TypeUtils.ResolveType(orset.TypeInfo.TypeName);

            var eOther = orset.OtherElements.Zip(dots,
                (i, versionVector) => (_ser.OtherMessageFromProto(i), versionVector))
                .ToImmutableDictionary(x => x.Item1, x => x.versionVector);

            var setType = ORSetMaker.MakeGenericMethod(setContentType);
            return (IORSet)setType.Invoke(this, new object[] { eOther, vector });
        }

        private static readonly MethodInfo ORSetMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(ToGenericORSet), BindingFlags.Static | BindingFlags.NonPublic);

        private static ORSet<T> ToGenericORSet<T>(ImmutableDictionary<object, VersionVector> elems, VersionVector vector)
        {
            var finalInput = elems.ToImmutableDictionary(x => (T)x.Key, v => v.Value);

            return new ORSet<T>(finalInput, vector);
        }

        private static readonly MethodInfo ORSetUnknownMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(ORSetUnknownToProto), BindingFlags.Instance | BindingFlags.NonPublic);

        /// <summary>
        /// Called when we're serializing none of the standard object types with ORSet
        /// </summary>
        private Protocol.ORSet ORSetUnknownToProto<T>(IORSet o)
        {
            var orset = (ORSet<T>)o;
            var p = ORSetToProto(orset);
            p.TypeInfo = new Protocol.TypeDescriptor(Protocol.ValType.Other, RuntimeTypeNameFormatter.Format(typeof(T)));
            p.OtherElements = orset.Elements.Select(x => _ser.OtherMessageToProto(x)).ToList();
            return p;
        }

        private ORSet.IAddDeltaOperation ORAddDeltaOperationFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var set = FromProto(MessagePackSerializer.Deserialize<Protocol.ORSet>(bytes, s_defaultResolver));
            return set.ToAddDeltaOperation();
        }

        private ORSet.IRemoveDeltaOperation ORRemoveOperationFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var set = FromProto(MessagePackSerializer.Deserialize<Protocol.ORSet>(bytes, s_defaultResolver));
            return set.ToRemoveDeltaOperation();
        }

        private ORSet.IFullStateDeltaOperation ORFullStateDeltaOperationFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var set = FromProto(MessagePackSerializer.Deserialize<Protocol.ORSet>(bytes, s_defaultResolver));
            return set.ToFullStateDeltaOperation();
        }

        private Protocol.ORSetDeltaGroup ToProto(ORSet.IDeltaGroupOperation orset)
        {
            var gatheredTypeInfo = false;
            Protocol.TypeDescriptor typeInfo = default;
            var entries = new List<Protocol.ORSetDeltaGroup.Entry>();

            void SetType(IORSet underlying)
            {
                if (!gatheredTypeInfo) // only need to do this once - all Deltas must have ORSet<T> of same <T>
                {
                    typeInfo = GetTypeDescriptor(underlying.SetType);
                }
                gatheredTypeInfo = true;
            }

            foreach (var op in orset.OperationsSerialization)
            {
                switch (op)
                {
                    case ORSet.IAddDeltaOperation add:
                        entries.Add(new Protocol.ORSetDeltaGroup.Entry(Protocol.ORSetDeltaOp.Add, ToProto(add.UnderlyingSerialization)));
                        SetType(add.UnderlyingSerialization);
                        break;
                    case ORSet.IRemoveDeltaOperation remove:
                        entries.Add(new Protocol.ORSetDeltaGroup.Entry(Protocol.ORSetDeltaOp.Remove, ToProto(remove.UnderlyingSerialization)));
                        SetType(remove.UnderlyingSerialization);
                        break;
                    case ORSet.IFullStateDeltaOperation full:
                        entries.Add(new Protocol.ORSetDeltaGroup.Entry(Protocol.ORSetDeltaOp.Full, ToProto(full.UnderlyingSerialization)));
                        SetType(full.UnderlyingSerialization);
                        break;
                    default: throw GetShouldNotBeNestedException(op);
                }
            }

            return new Protocol.ORSetDeltaGroup(entries, typeInfo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentException GetShouldNotBeNestedException(IReplicatedData op)
        {
            return new ArgumentException($"{op} should not be nested");
        }

        private ORSet.IDeltaGroupOperation ORDeltaGroupOperationFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var deltaGroup = MessagePackSerializer.Deserialize<Protocol.ORSetDeltaGroup>(bytes, s_defaultResolver);
            var ops = new List<ORSet.IDeltaOperation>();

            for (int idx = 0; idx < deltaGroup.Entries.Count; idx++)
            {
                var op = deltaGroup.Entries[idx];
                switch (op.Operation)
                {
                    case Protocol.ORSetDeltaOp.Add:
                        ops.Add(FromProto(op.Underlying).ToAddDeltaOperation());
                        break;
                    case Protocol.ORSetDeltaOp.Remove:
                        ops.Add(FromProto(op.Underlying).ToRemoveDeltaOperation());
                        break;
                    case Protocol.ORSetDeltaOp.Full:
                        ops.Add(FromProto(op.Underlying).ToFullStateDeltaOperation());
                        break;
                    default:
                        throw GetUnknownORSetDeltaOperationException(op.Operation);

                }
            }

            var arr = ops.Cast<IReplicatedData>().ToImmutableArray();

            switch (deltaGroup.TypeInfo.Type)
            {
                case Protocol.ValType.Int:
                    return new ORSet<int>.DeltaGroup(arr);
                case Protocol.ValType.Long:
                    return new ORSet<long>.DeltaGroup(arr);
                case Protocol.ValType.String:
                    return new ORSet<string>.DeltaGroup(arr);
                case Protocol.ValType.ActorRef:
                    return new ORSet<IActorRef>.DeltaGroup(arr);
            }

            // if we made it this far, we're working with an object type
            // enter reflection magic

            var type = TypeUtils.ResolveType(deltaGroup.TypeInfo.TypeName);
            var orDeltaGroupType = typeof(ORSet<>.DeltaGroup).GetCachedGenericType(type);
            return (ORSet.IDeltaGroupOperation)Activator.CreateInstance(orDeltaGroupType, arr);
        }

        private static SerializationException GetUnknownORSetDeltaOperationException(Protocol.ORSetDeltaOp operation)
        {
            return new SerializationException($"Unknown ORSet delta operation ${operation}");
        }

        #endregion

        #region GSet

        [MethodImpl(InlineOptions.AggressiveOptimization)]
        private Protocol.GSet GSetToProto<T>(/*GSet<T> gset*/)
        {
            return new Protocol.GSet
            {
                TypeInfo = GetTypeDescriptor(typeof(T))
            };
        }

        private Protocol.GSet GSetToProtoUnknown<T>(IGSet g)
        {
            var gset = (GSet<T>)g;
            return new Protocol.GSet
            {
                TypeInfo = GetTypeDescriptor(typeof(T)),
                OtherElements = gset.Select(x => _ser.OtherMessageToProto(x)).ToList()
            };
        }

        private static readonly MethodInfo GSetUnknownToProtoMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(GSetToProtoUnknown), BindingFlags.Instance | BindingFlags.NonPublic);

        private Protocol.GSet ToProto(IGSet gset)
        {
            switch (gset)
            {
                case GSet<int> ints:
                    {
                        var p = GSetToProto<int>(/*ints*/);
                        p.IntElements = ints.Elements.ToList();
                        return p;
                    }
                case GSet<long> longs:
                    {
                        var p = GSetToProto<long>(/*longs*/);
                        p.LongElements = longs.Elements.ToList();
                        return p;
                    }
                case GSet<string> strings:
                    {
                        var p = GSetToProto<string>(/*strings*/);
                        p.StringElements = strings.Elements.ToList();
                        return p;
                    }
                case GSet<IActorRef> refs:
                    {
                        var p = GSetToProto<IActorRef>(/*refs*/);
                        p.ActorRefElements = refs.Select(Akka.Serialization.Serialization.SerializedActorPath).ToList();
                        return p;
                    }
                default: // unknown type
                    {
                        var protoMaker = GSetUnknownToProtoMaker.MakeGenericMethod(gset.SetType);
                        return (Protocol.GSet)protoMaker.Invoke(this, new object[] { gset });
                    }
            }
        }

        private IGSet GSetFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var gset = MessagePackSerializer.Deserialize<Protocol.GSet>(bytes, s_defaultResolver);

            switch (gset.TypeInfo.Type)
            {
                case Protocol.ValType.Int:
                    {
                        var eInt = gset.IntElements.ToImmutableHashSet();

                        return new GSet<int>(eInt);
                    }
                case Protocol.ValType.Long:
                    {
                        var eLong = gset.LongElements.ToImmutableHashSet();

                        return new GSet<long>(eLong);
                    }
                case Protocol.ValType.String:
                    {
                        var eStr = gset.StringElements.ToImmutableHashSet();
                        return new GSet<string>(eStr);
                    }
                case Protocol.ValType.ActorRef:
                    {
                        var eRef = gset.ActorRefElements.Select(x => _ser.ResolveActorRef(x)).ToImmutableHashSet();
                        return new GSet<IActorRef>(eRef);
                    }
                case Protocol.ValType.Other:
                    {
                        // runtime type - enter horrible dynamic serialization stuff

                        var setContentType = TypeUtils.ResolveType(gset.TypeInfo.TypeName);

                        var eOther = gset.OtherElements.Select(x => _ser.OtherMessageFromProto(x));

                        var setType = GSetMaker.MakeGenericMethod(setContentType);
                        return (IGSet)setType.Invoke(this, new object[] { eOther });
                    }
                default:
                    throw GetUnknownValTypeOfDetectedWhileDeserializingGSetException(gset.TypeInfo.Type);
            }
        }

        [MethodImpl(InlineOptions.AggressiveOptimization)]
        private static SerializationException GetUnknownValTypeOfDetectedWhileDeserializingGSetException(Protocol.ValType vt)
        {
            return new SerializationException($"Unknown ValType of [{vt}] detected while deserializing GSet");
        }

        private static readonly MethodInfo GSetMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(ToGenericGSet), BindingFlags.Static | BindingFlags.NonPublic);

        private static GSet<T> ToGenericGSet<T>(IEnumerable<object> items)
        {
            return new GSet<T>(items.Cast<T>().ToImmutableHashSet());
        }

        #endregion

        #region GCounter

        private Protocol.GCounter ToProto(GCounter counter)
        {
            var entries = counter.State.Select(x => new Protocol.GCounter.Entry(SerializationSupport.UniqueAddressToProto(x.Key), BitConverter.GetBytes(x.Value))).ToList();

            return new Protocol.GCounter(entries);
        }

        private GCounter GCounterFromBytes(in ReadOnlySpan<byte> bytes)
        {
            var gProto = MessagePackSerializer.Deserialize<Protocol.GCounter>(bytes, s_defaultResolver);

            return GCounterFromProto(gProto);
        }

        private GCounter GCounterFromProto(in Protocol.GCounter gProto)
        {
            var entries = gProto.Entries.ToImmutableDictionary(k => _ser.UniqueAddressFromProto(k.Node),
                v => BitConverter.ToUInt64(v.Value, 0));

            return new GCounter(entries);
        }

        #endregion

        #region PNCounter

        private Protocol.PNCounter ToProto(PNCounter counter)
        {
            return new Protocol.PNCounter(ToProto(counter.Increments), ToProto(counter.Decrements));
        }

        private PNCounter PNCounterFromBytes(in ReadOnlySpan<byte> bytes)
        {
            var pProto = MessagePackSerializer.Deserialize<Protocol.PNCounter>(bytes, s_defaultResolver);
            return PNCounterFromProto(pProto);
        }

        private PNCounter PNCounterFromProto(in Protocol.PNCounter pProto)
        {
            var increments = GCounterFromProto(pProto.Increments);
            var decrements = GCounterFromProto(pProto.Decrements);

            return new PNCounter(increments, decrements);
        }

        #endregion

        #region Flag

        private Protocol.Flag ToProto(Flag flag)
        {
            return new Protocol.Flag(flag);
        }

        private Flag FlagFromProto(in Protocol.Flag flag)
        {
            return flag.Enabled ? Flag.True : Flag.False;
        }

        private Flag FlagFromBinary(in ReadOnlySpan<byte> bytes)
        {
            return FlagFromProto(MessagePackSerializer.Deserialize<Protocol.Flag>(bytes, s_defaultResolver));
        }

        #endregion

        #region LWWRegister

        private Protocol.LWWRegister ToProto(ILWWRegister register)
        {
            var protoMaker = LWWProtoMaker.MakeGenericMethod(register.RegisterType);
            return (Protocol.LWWRegister)protoMaker.Invoke(this, new object[] { register });
        }

        private static readonly MethodInfo LWWProtoMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(LWWToProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private Protocol.LWWRegister LWWToProto<T>(ILWWRegister r)
        {
            var register = (LWWRegister<T>)r;
            return new Protocol.LWWRegister(
                register.Timestamp,
                SerializationSupport.UniqueAddressToProto(register.UpdatedBy),
                _ser.OtherMessageToProto(register.Value),
                GetTypeDescriptor(r.RegisterType)
            );
        }

        private ILWWRegister LWWRegisterFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.LWWRegister>(bytes, s_defaultResolver);
            return LWWRegisterFromProto(proto);
        }

        private ILWWRegister LWWRegisterFromProto(Protocol.LWWRegister proto)
        {
            var vt = proto.TypeInfo.Type;
            switch (vt)
            {
                case Protocol.ValType.Int:
                    {
                        return GenericLWWRegisterFromProto<int>(proto);
                    }
                case Protocol.ValType.Long:
                    {
                        return GenericLWWRegisterFromProto<long>(proto);
                    }
                case Protocol.ValType.String:
                    {
                        return GenericLWWRegisterFromProto<string>(proto);
                    }
                case Protocol.ValType.ActorRef:
                    {
                        return GenericLWWRegisterFromProto<IActorRef>(proto);
                    }
                case Protocol.ValType.Other:
                    {
                        // runtime type - enter horrible dynamic serialization stuff

                        var setContentType = TypeUtils.ResolveType(proto.TypeInfo.TypeName);

                        var setType = LWWRegisterMaker.MakeGenericMethod(setContentType);
                        return (ILWWRegister)setType.Invoke(this, new object[] { proto });
                    }
                default:
                    throw GetSerializationException_Deserializing_LWWRegister(vt);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SerializationException GetSerializationException_Deserializing_LWWRegister(Protocol.ValType vt)
        {
            return new SerializationException($"Unknown ValType of [{vt}] detected while deserializing LWWRegister");
        }

        private static readonly MethodInfo LWWRegisterMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(GenericLWWRegisterFromProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private LWWRegister<T> GenericLWWRegisterFromProto<T>(Protocol.LWWRegister proto)
        {
            var msg = (T)_ser.OtherMessageFromProto(proto.State);
            var updatedBy = _ser.UniqueAddressFromProto(proto.Node);

            return new LWWRegister<T>(updatedBy, msg, proto.Timestamp);
        }

        #endregion

        #region ORMap

        private Protocol.ORMap ToProto(IORDictionary ormap)
        {
            var protoMaker = ORDictProtoMaker.MakeGenericMethod(ormap.KeyType, ormap.ValueType);
            return (Protocol.ORMap)protoMaker.Invoke(this, new object[] { ormap });
        }

        private static readonly MethodInfo ORDictProtoMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(ORDictToProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private Protocol.ORMap ORDictToProto<TKey, TValue>(IORDictionary o) where TValue : IReplicatedData<TValue>
        {
            var ormap = (ORDictionary<TKey, TValue>)o;
            var proto = new Protocol.ORMap();
            ToORMapEntries(ormap.Entries, proto);
            proto.Keys = ToProto(ormap.KeySet);
            proto.ValueTypeInfo = GetTypeDescriptor(typeof(TValue));
            return proto;
        }

        private void ToORMapEntries<TKey, TValue>(IImmutableDictionary<TKey, TValue> ormapEntries, in Protocol.ORMap proto) where TValue : IReplicatedData<TValue>
        {
            var entries = new List<Protocol.ORMap.Entry>();
            foreach (var e in ormapEntries)
            {
                var entry = new Protocol.ORMap.Entry();
                switch (e.Key)
                {
                    case int i:
                        entry.IntKey = i;
                        break;
                    case long l:
                        entry.LongKey = l;
                        break;
                    case string str:
                        entry.StringKey = str;
                        break;
                    default:
                        entry.OtherKey = _ser.OtherMessageToProto(e.Key);
                        break;
                }

                entry.Value = _ser.OtherMessageToProto(e.Value);
                entries.Add(entry);
            }
            proto.Entries = entries;
        }

        private static readonly MethodInfo ORDictMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(GenericORDictionaryFromProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private IORDictionary ORDictionaryFromLZ4Binary(in ReadOnlySpan<byte> bytes)
        {
            var proto = LZ4MessagePackSerializer.Deserialize<Protocol.ORMap>(bytes, s_defaultResolver);
            return ORDictionaryFromProto(proto);
        }

        private IORDictionary ORDictionaryFromProto(Protocol.ORMap proto)
        {
            var keyType = GetTypeFromDescriptor(proto.Keys.TypeInfo);
            var valueType = GetTypeFromDescriptor(proto.ValueTypeInfo);
            var protoMaker = ORDictMaker.MakeGenericMethod(keyType, valueType);
            return (IORDictionary)protoMaker.Invoke(this, new object[] { proto });
        }

        private IORDictionary GenericORDictionaryFromProto<TKey, TValue>(Protocol.ORMap proto) where TValue : IReplicatedData<TValue>
        {
            var keys = FromProto(proto.Keys);
            switch (proto.Keys.TypeInfo.Type)
            {
                case Protocol.ValType.Int:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => x.IntKey,
                            v => (TValue)_ser.OtherMessageFromProto(v.Value));
                        return new ORDictionary<int, TValue>((ORSet<int>)keys, entries);
                    }
                case Protocol.ValType.Long:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => x.LongKey,
                            v => (TValue)_ser.OtherMessageFromProto(v.Value));
                        return new ORDictionary<long, TValue>((ORSet<long>)keys, entries);
                    }
                case Protocol.ValType.String:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => x.StringKey,
                            v => (TValue)_ser.OtherMessageFromProto(v.Value));
                        return new ORDictionary<string, TValue>((ORSet<string>)keys, entries);
                    }
                default:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => (TKey)_ser.OtherMessageFromProto(x.OtherKey),
                            v => (TValue)_ser.OtherMessageFromProto(v.Value));

                        return new ORDictionary<TKey, TValue>((ORSet<TKey>)keys, entries);
                    }
            }
        }

        private Protocol.ORMapDeltaGroup ORDictionaryDeltasToProto(
            List<ORDictionary.IDeltaOperation> deltaGroupOps)
        {
            var keyType = deltaGroupOps[0].KeyType;
            var valueType = deltaGroupOps[0].ValueType;

            var protoMaker = ORDeltaGroupProtoMaker.MakeGenericMethod(keyType, valueType);
            return (Protocol.ORMapDeltaGroup)protoMaker.Invoke(this, new object[] { deltaGroupOps });
        }

        private static readonly MethodInfo ORDeltaGroupProtoMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(ORDictionaryDeltaGroupToProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private Protocol.ORMapDeltaGroup ORDictionaryDeltaGroupToProto<TKey, TValue>(
            List<ORDictionary.IDeltaOperation> deltaGroupOps) where TValue : IReplicatedData<TValue>
        {
            var group = new Protocol.ORMapDeltaGroup
            {
                KeyTypeInfo = GetTypeDescriptor(typeof(TKey)),
                ValueTypeInfo = GetTypeDescriptor(typeof(TValue))
            };

            Protocol.ORMapDeltaGroup.MapEntry CreateMapEntry(TKey key, object value = null)
            {
                var entry = new Protocol.ORMapDeltaGroup.MapEntry();
                switch (key)
                {
                    case int i:
                        entry.IntKey = i;
                        break;
                    case long l:
                        entry.LongKey = l;
                        break;
                    case string s:
                        entry.StringKey = s;
                        break;
                    default:
                        entry.OtherKey = _ser.OtherMessageToProto(key);
                        break;
                }

                entry.Value = _ser.OtherMessageToProto(value);
                return entry;
            }

            Protocol.ORMapDeltaGroup.Entry CreateEntry(ORDictionary<TKey, TValue>.IDeltaOperation op)
            {
                var entry = new Protocol.ORMapDeltaGroup.Entry();
                switch (op)
                {
                    case ORDictionary<TKey, TValue>.PutDeltaOperation putDelta:
                        entry.Operation = Protocol.ORMapDeltaOp.ORMapPut;
                        entry.Underlying = ToProto(putDelta.Underlying.AsInstanceOf<ORSet.IDeltaOperation>()
                            .UnderlyingSerialization);
                        entry.EntryData.Add(CreateMapEntry(putDelta.Key, putDelta.Value));
                        break;
                    case ORDictionary<TKey, TValue>.UpdateDeltaOperation upDelta:
                        entry.Operation = Protocol.ORMapDeltaOp.ORMapUpdate;
                        entry.Underlying = ToProto(upDelta.Underlying.AsInstanceOf<ORSet.IDeltaOperation>()
                            .UnderlyingSerialization);
                        entry.EntryData.AddRange(upDelta.Values.Select(x => CreateMapEntry(x.Key, x.Value)).ToList());
                        break;
                    case ORDictionary<TKey, TValue>.RemoveDeltaOperation removeDelta:
                        entry.Operation = Protocol.ORMapDeltaOp.ORMapRemove;
                        entry.Underlying = ToProto(removeDelta.Underlying.AsInstanceOf<ORSet.IDeltaOperation>()
                            .UnderlyingSerialization);
                        break;
                    case ORDictionary<TKey, TValue>.RemoveKeyDeltaOperation removeKeyDelta:
                        entry.Operation = Protocol.ORMapDeltaOp.ORMapRemoveKey;
                        entry.Underlying = ToProto(removeKeyDelta.Underlying.AsInstanceOf<ORSet.IDeltaOperation>()
                            .UnderlyingSerialization);
                        entry.EntryData.Add(CreateMapEntry(removeKeyDelta.Key));
                        break;
                    default:
                        throw GetUnknownORDictionaryException(op.GetType());
                }

                return entry;
            }

            group.Entries = deltaGroupOps.Cast<ORDictionary<TKey, TValue>.IDeltaOperation>().Select(x => CreateEntry(x)).ToList();
            return group;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SerializationException GetUnknownORDictionaryException(Type t)
        {
            return new SerializationException($"Unknown ORDictionary delta type {t}");
        }

        private Protocol.ORMapDeltaGroup ToProto(ORDictionary.IDeltaOperation op)
        {
            switch (op)
            {
                case ORDictionary.IPutDeltaOp p: return ORDictionaryPutToProto(p);
                case ORDictionary.IRemoveDeltaOp r: return ORDictionaryRemoveToProto(r);
                case ORDictionary.IRemoveKeyDeltaOp r: return ORDictionaryRemoveKeyToProto(r);
                case ORDictionary.IUpdateDeltaOp u: return ORDictionaryUpdateToProto(u);
                case ORDictionary.IDeltaGroupOp g: return ORDictionaryDeltasToProto(g.OperationsSerialization.ToList());
                default:
                    throw GetUnrecognizedDeltaOperationException(op);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SerializationException GetUnrecognizedDeltaOperationException(ORDictionary.IDeltaOperation op)
        {
            return new SerializationException($"Unrecognized delta operation [{op}]");
        }

        private Protocol.ORMapDeltaGroup ORDictionaryPutToProto(ORDictionary.IPutDeltaOp op)
        {
            return ORDictionaryDeltasToProto(new List<ORDictionary.IDeltaOperation>() { op });
        }

        private Protocol.ORMapDeltaGroup ORDictionaryRemoveToProto(ORDictionary.IRemoveDeltaOp op)
        {
            return ORDictionaryDeltasToProto(new List<ORDictionary.IDeltaOperation>() { op });
        }

        private Protocol.ORMapDeltaGroup ORDictionaryRemoveKeyToProto(ORDictionary.IRemoveKeyDeltaOp op)
        {
            return ORDictionaryDeltasToProto(new List<ORDictionary.IDeltaOperation>() { op });
        }

        private Protocol.ORMapDeltaGroup ORDictionaryUpdateToProto(ORDictionary.IUpdateDeltaOp op)
        {
            return ORDictionaryDeltasToProto(new List<ORDictionary.IDeltaOperation>() { op });
        }

        private ORDictionary.IDeltaGroupOp ORDictionaryDeltaGroupFromProto(Protocol.ORMapDeltaGroup deltaGroup)
        {
            var keyType = GetTypeFromDescriptor(deltaGroup.KeyTypeInfo);
            var valueType = GetTypeFromDescriptor(deltaGroup.ValueTypeInfo);

            var groupMaker = ORDeltaGroupMaker.MakeGenericMethod(keyType, valueType);
            return (ORDictionary.IDeltaGroupOp)groupMaker.Invoke(this, new object[] { deltaGroup });
        }

        private static readonly MethodInfo ORDeltaGroupMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(GenericORDictionaryDeltaGroupFromProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private ORDictionary.IDeltaGroupOp GenericORDictionaryDeltaGroupFromProto<TKey, TValue>(Protocol.ORMapDeltaGroup deltaGroup) where TValue : IReplicatedData<TValue>
        {
            var deltaOps = new List<ORDictionary<TKey, TValue>.IDeltaOperation>();

            (object key, object value) MapEntryFromProto(Protocol.ORMapDeltaGroup.MapEntry entry)
            {
                object k = null;
                switch (deltaGroup.KeyTypeInfo.Type)
                {
                    case Protocol.ValType.Int:
                        k = entry.IntKey;
                        break;
                    case Protocol.ValType.Long:
                        k = entry.LongKey;
                        break;
                    case Protocol.ValType.String:
                        k = entry.StringKey;
                        break;
                    default:
                        k = _ser.OtherMessageFromProto(entry.OtherKey);
                        break;
                }

                return (k, _ser.OtherMessageFromProto(entry.Value));
            }

            foreach (var entry in deltaGroup.Entries)
            {
                var underlying = FromProto(entry.Underlying);
                switch (entry.Operation)
                {
                    case Protocol.ORMapDeltaOp.ORMapPut:
                        {
                            if (entry.EntryData.Count > 1)
                            {
                                ThrowArgumentOutOfRangeException_Deserialize_ORDictionary();
                            }
                            var (key, value) = MapEntryFromProto(entry.EntryData[0]);

                            deltaOps.Add(new ORDictionary<TKey, TValue>.PutDeltaOperation(new ORSet<TKey>.AddDeltaOperation((ORSet<TKey>)underlying), (TKey)key, (TValue)value));
                        }
                        break;
                    case Protocol.ORMapDeltaOp.ORMapRemove:
                        {
                            deltaOps.Add(new ORDictionary<TKey, TValue>.RemoveDeltaOperation(new ORSet<TKey>.RemoveDeltaOperation((ORSet<TKey>)underlying)));
                        }
                        break;
                    case Protocol.ORMapDeltaOp.ORMapRemoveKey:
                        {
                            if (entry.EntryData.Count > 1)
                            {
                                ThrowArgumentOutOfRangeException_Deserialize_ORDictionary();
                            }
                            var (key, value) = MapEntryFromProto(entry.EntryData[0]);
                            deltaOps.Add(new ORDictionary<TKey, TValue>.RemoveKeyDeltaOperation(new ORSet<TKey>.RemoveDeltaOperation((ORSet<TKey>)underlying), (TKey)key));
                        }
                        break;
                    case Protocol.ORMapDeltaOp.ORMapUpdate:
                        {
                            var entries = entry.EntryData.Select(x => MapEntryFromProto(x))
                                .ToImmutableDictionary(x => (TKey)x.key, v => (IReplicatedData)v.value);
                            deltaOps.Add(new ORDictionary<TKey, TValue>.UpdateDeltaOperation(new ORSet<TKey>.AddDeltaOperation((ORSet<TKey>)underlying), entries));
                        }
                        break;
                    default:
                        throw GetUnknownORDictionaryDeltaOperationException(entry.Operation);
                }
            }

            return new ORDictionary<TKey, TValue>.DeltaGroup(deltaOps);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentOutOfRangeException_Deserialize_ORDictionary()
        {
            throw GetException();
            static ArgumentOutOfRangeException GetException()
            {
                return new ArgumentOutOfRangeException("Can't deserialize key/value pair in ORDictionary delta - too many pairs on the wire");
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SerializationException GetUnknownORDictionaryDeltaOperationException(Protocol.ORMapDeltaOp operation)
        {
            return new SerializationException($"Unknown ORDictionary delta operation ${operation}");
        }

        private ORDictionary.IDeltaGroupOp ORDictionaryDeltaGroupFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var group = MessagePackSerializer.Deserialize<Protocol.ORMapDeltaGroup>(bytes, s_defaultResolver);
            return ORDictionaryDeltaGroupFromProto(group);
        }

        private ORDictionary.IPutDeltaOp ORDictionaryPutFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var groupOp = ORDictionaryDeltaGroupFromBinary(bytes);
            if (groupOp.OperationsSerialization.Count == 1 &&
                groupOp.OperationsSerialization.First() is ORDictionary.IPutDeltaOp put)
            {
                return put;
            }
            throw GetImproperORDictionaryDeltaPutOperationSizeOrKindException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SerializationException GetImproperORDictionaryDeltaPutOperationSizeOrKindException()
        {
            return new SerializationException("Improper ORDictionary delta put operation size or kind");
        }

        private ORDictionary.IRemoveDeltaOp ORDictionaryRemoveFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var groupOp = ORDictionaryDeltaGroupFromBinary(bytes);
            if (groupOp.OperationsSerialization.Count == 1 &&
                groupOp.OperationsSerialization.First() is ORDictionary.IRemoveDeltaOp remove)
            {
                return remove;
            }
            throw GetImproperORDictionaryDeltaRemoveOperationSizeOrKindException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SerializationException GetImproperORDictionaryDeltaRemoveOperationSizeOrKindException()
        {
            return new SerializationException("Improper ORDictionary delta remove operation size or kind");
        }

        private ORDictionary.IRemoveKeyDeltaOp ORDictionaryRemoveKeyFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var groupOp = ORDictionaryDeltaGroupFromBinary(bytes);
            if (groupOp.OperationsSerialization.Count == 1 &&
                groupOp.OperationsSerialization.First() is ORDictionary.IRemoveKeyDeltaOp removeKey)
            {
                return removeKey;
            }
            throw GetImproperORDictionaryDeltaRemoveKeyOperationSizeOrKindException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SerializationException GetImproperORDictionaryDeltaRemoveKeyOperationSizeOrKindException()
        {
            return new SerializationException("Improper ORDictionary delta remove key operation size or kind");
        }

        private ORDictionary.IUpdateDeltaOp ORDictionaryUpdateFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var groupOp = ORDictionaryDeltaGroupFromBinary(bytes);
            if (groupOp.OperationsSerialization.Count == 1 &&
                groupOp.OperationsSerialization.First() is ORDictionary.IUpdateDeltaOp update)
            {
                return update;
            }
            throw GetImproperORDictionaryDeltaUpdateOperationSizeOrKindException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SerializationException GetImproperORDictionaryDeltaUpdateOperationSizeOrKindException()
        {
            return new SerializationException("Improper ORDictionary delta update operation size or kind");
        }

        #endregion

        #region LWWDictionary

        private Protocol.LWWMap ToProto(ILWWDictionary lwwDictionary)
        {
            var protoMaker = LWWDictProtoMaker.MakeGenericMethod(lwwDictionary.KeyType, lwwDictionary.ValueType);
            return (Protocol.LWWMap)protoMaker.Invoke(this, new object[] { lwwDictionary });
        }

        private static readonly MethodInfo LWWDictProtoMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(LWWDictToProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private Protocol.LWWMap LWWDictToProto<TKey, TValue>(ILWWDictionary o)
        {
            var lwwmap = (LWWDictionary<TKey, TValue>)o;
            var proto = new Protocol.LWWMap();
            ToLWWMapEntries(lwwmap.Underlying.Entries, proto);
            proto.Keys = ToProto(lwwmap.Underlying.KeySet);
            proto.ValueTypeInfo = GetTypeDescriptor(typeof(TValue));
            return proto;
        }

        private void ToLWWMapEntries<TKey, TValue>(IImmutableDictionary<TKey, LWWRegister<TValue>> underlyingEntries, Protocol.LWWMap proto)
        {
            var entries = new List<Protocol.LWWMap.Entry>(underlyingEntries.Count);
            foreach (var e in underlyingEntries)
            {
                var thisEntry = new Protocol.LWWMap.Entry();
                switch (e.Key)
                {
                    case int i:
                        thisEntry.IntKey = i;
                        break;
                    case long l:
                        thisEntry.LongKey = l;
                        break;
                    case string str:
                        thisEntry.StringKey = str;
                        break;
                    default:
                        thisEntry.OtherKey = _ser.OtherMessageToProto(e.Key);
                        break;
                }

                thisEntry.Value = LWWToProto<TValue>(e.Value);
                entries.Add(thisEntry);
            }

            proto.Entries = entries;
        }

        private static readonly MethodInfo LWWDictMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(GenericLWWDictFromProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private ILWWDictionary LWWDictFromProto(Protocol.LWWMap proto)
        {
            var keyType = GetTypeFromDescriptor(proto.Keys.TypeInfo);
            var valueType = GetTypeFromDescriptor(proto.ValueTypeInfo);

            var dictMaker = LWWDictMaker.MakeGenericMethod(keyType, valueType);
            return (ILWWDictionary)dictMaker.Invoke(this, new object[] { proto });
        }

        private ILWWDictionary GenericLWWDictFromProto<TKey, TValue>(Protocol.LWWMap proto)
        {
            var keys = FromProto(proto.Keys);
            switch (proto.Keys.TypeInfo.Type)
            {
                case Protocol.ValType.Int:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => x.IntKey,
                            v => GenericLWWRegisterFromProto<TValue>(v.Value));
                        var orDict = new ORDictionary<int, LWWRegister<TValue>>((ORSet<int>)keys, entries);
                        return new LWWDictionary<int, TValue>(orDict);
                    }
                case Protocol.ValType.Long:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => x.LongKey,
                            v => GenericLWWRegisterFromProto<TValue>(v.Value));
                        var orDict = new ORDictionary<long, LWWRegister<TValue>>((ORSet<long>)keys, entries);
                        return new LWWDictionary<long, TValue>(orDict);
                    }
                case Protocol.ValType.String:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => x.StringKey,
                            v => GenericLWWRegisterFromProto<TValue>(v.Value));
                        var orDict = new ORDictionary<string, LWWRegister<TValue>>((ORSet<string>)keys, entries);
                        return new LWWDictionary<string, TValue>(orDict);
                    }
                default:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => (TKey)_ser.OtherMessageFromProto(x.OtherKey),
                            v => GenericLWWRegisterFromProto<TValue>(v.Value));
                        var orDict = new ORDictionary<TKey, LWWRegister<TValue>>((ORSet<TKey>)keys, entries);
                        return new LWWDictionary<TKey, TValue>(orDict);
                    }
            }
        }

        private ILWWDictionary LWWDictionaryFromLZ4Binary(in ReadOnlySpan<byte> bytes)
        {
            var proto = LZ4MessagePackSerializer.Deserialize<Protocol.LWWMap>(bytes, s_defaultResolver);
            return LWWDictFromProto(proto);
        }

        private object LWWDictionaryDeltaGroupFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.ORMapDeltaGroup>(bytes, s_defaultResolver);
            var orDictOp = ORDictionaryDeltaGroupFromProto(proto);

            var orSetType = orDictOp.ValueType.GenericTypeArguments[0];
            var maker = LWWDictionaryDeltaMaker.MakeGenericMethod(orDictOp.KeyType, orSetType);
            return (ILWWDictionaryDeltaOperation)maker.Invoke(this, new object[] { orDictOp });
        }

        private static readonly MethodInfo LWWDictionaryDeltaMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(LWWDictionaryDeltaFromProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private ILWWDictionaryDeltaOperation LWWDictionaryDeltaFromProto<TKey, TValue>(ORDictionary.IDeltaOperation op)
        {
            var casted = (ORDictionary<TKey, LWWRegister<TValue>>.IDeltaOperation)op;
            return new LWWDictionary<TKey, TValue>.LWWDictionaryDelta(casted);
        }

        #endregion

        #region PNCounterDictionary

        private Protocol.PNCounterMap ToProto(IPNCounterDictionary pnCounterDictionary)
        {
            var protoMaker = PNCounterDictProtoMaker.MakeGenericMethod(pnCounterDictionary.KeyType);
            return (Protocol.PNCounterMap)protoMaker.Invoke(this, new object[] { pnCounterDictionary });
        }

        private static readonly MethodInfo PNCounterDictProtoMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(GenericPNCounterDictionaryToProto), BindingFlags.Instance | BindingFlags.NonPublic);


        private Protocol.PNCounterMap GenericPNCounterDictionaryToProto<TKey>(IPNCounterDictionary pnCounterDictionary)
        {
            var pnDict = (PNCounterDictionary<TKey>)pnCounterDictionary;
            var proto = new Protocol.PNCounterMap();
            proto.Keys = ToProto(pnDict.Underlying.KeySet);
            ToPNCounterEntries(pnDict.Underlying.Entries, proto);
            return proto;
        }

        private void ToPNCounterEntries<TKey>(IImmutableDictionary<TKey, PNCounter> underlyingEntries, Protocol.PNCounterMap proto)
        {
            var entries = new List<Protocol.PNCounterMap.Entry>();
            foreach (var e in underlyingEntries)
            {
                var thisEntry = new Protocol.PNCounterMap.Entry();
                switch (e.Key)
                {
                    case int i:
                        thisEntry.IntKey = i;
                        break;
                    case long l:
                        thisEntry.LongKey = l;
                        break;
                    case string str:
                        thisEntry.StringKey = str;
                        break;
                    default:
                        thisEntry.OtherKey = _ser.OtherMessageToProto(e.Key);
                        break;
                }

                thisEntry.Value = ToProto(e.Value);
                entries.Add(thisEntry);
            }

            proto.Entries = entries;
        }

        private IPNCounterDictionary PNCounterDictionaryFromLZ4Binary(in ReadOnlySpan<byte> bytes)
        {
            var proto = LZ4MessagePackSerializer.Deserialize<Protocol.PNCounterMap>(bytes, s_defaultResolver);
            return PNCounterDictionaryFromProto(proto);
        }

        private IPNCounterDictionary PNCounterDictionaryFromProto(Protocol.PNCounterMap proto)
        {
            var keyType = GetTypeFromDescriptor(proto.Keys.TypeInfo);
            var dictMaker = PNCounterDictMaker.MakeGenericMethod(keyType);
            return (IPNCounterDictionary)dictMaker.Invoke(this, new object[] { proto });
        }

        private static readonly MethodInfo PNCounterDictMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(GenericPNCounterDictionaryFromProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private IPNCounterDictionary GenericPNCounterDictionaryFromProto<TKey>(Protocol.PNCounterMap proto)
        {
            var keys = FromProto(proto.Keys);
            switch (proto.Keys.TypeInfo.Type)
            {
                case Protocol.ValType.Int:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => x.IntKey,
                            v => PNCounterFromProto(v.Value));
                        var orDict = new ORDictionary<int, PNCounter>((ORSet<int>)keys, entries);
                        return new PNCounterDictionary<int>(orDict);
                    }
                case Protocol.ValType.Long:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => x.LongKey,
                            v => PNCounterFromProto(v.Value));
                        var orDict = new ORDictionary<long, PNCounter>((ORSet<long>)keys, entries);
                        return new PNCounterDictionary<long>(orDict);
                    }
                case Protocol.ValType.String:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => x.StringKey,
                            v => PNCounterFromProto(v.Value));
                        var orDict = new ORDictionary<string, PNCounter>((ORSet<string>)keys, entries);
                        return new PNCounterDictionary<string>(orDict);
                    }
                default:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => (TKey)_ser.OtherMessageFromProto(x.OtherKey),
                            v => PNCounterFromProto(v.Value));
                        var orDict = new ORDictionary<TKey, PNCounter>((ORSet<TKey>)keys, entries);
                        return new PNCounterDictionary<TKey>(orDict);
                    }
            }
        }

        private IPNCounterDictionaryDeltaOperation PNCounterDeltaFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.ORMapDeltaGroup>(bytes, s_defaultResolver);
            var orDictOp = ORDictionaryDeltaGroupFromProto(proto);
            var maker = PNCounterDeltaMaker.MakeGenericMethod(orDictOp.KeyType);
            return (IPNCounterDictionaryDeltaOperation)maker.Invoke(this, new object[] { orDictOp });
        }

        private static readonly MethodInfo PNCounterDeltaMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(PNCounterDeltaFromProto), BindingFlags.Instance | BindingFlags.NonPublic);


        private IPNCounterDictionaryDeltaOperation PNCounterDeltaFromProto<TKey>(ORDictionary.IDeltaOperation op)
        {
            var casted = (ORDictionary<TKey, PNCounter>.IDeltaOperation)op;
            return new PNCounterDictionary<TKey>.PNCounterDictionaryDelta(casted);
        }

        #endregion

        #region ORMultiDictionary

        private Protocol.ORMultiMap ToProto(IORMultiValueDictionary multi)
        {
            var protoMaker = MultiMapProtoMaker.MakeGenericMethod(multi.KeyType, multi.ValueType);
            return (Protocol.ORMultiMap)protoMaker.Invoke(this, new object[] { multi });
        }

        private static readonly MethodInfo MultiMapProtoMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(MultiMapToProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private Protocol.ORMultiMapDelta ToProto(IORMultiValueDictionaryDeltaOperation op)
        {
            return new Protocol.ORMultiMapDelta(ToProto(op.Underlying), op.WithValueDeltas);
        }


        private Protocol.ORMultiMap MultiMapToProto<TKey, TValue>(IORMultiValueDictionary multi)
        {
            var ormm = (ORMultiValueDictionary<TKey, TValue>)multi;
            var proto = new Protocol.ORMultiMap();
            proto.ValueTypeInfo = GetTypeDescriptor(typeof(TValue));
            if (ormm.DeltaValues)
            {
                proto.WithValueDeltas = true;
            }

            proto.Keys = ToProto(ormm.Underlying.KeySet);
            ToORMultiMapEntries(ormm.Underlying.Entries, proto);
            return proto;
        }

        private void ToORMultiMapEntries<TKey, TValue>(IImmutableDictionary<TKey, ORSet<TValue>> underlyingEntries, in Protocol.ORMultiMap proto)
        {
            var entries = new List<Protocol.ORMultiMap.Entry>();
            foreach (var e in underlyingEntries)
            {
                var thisEntry = new Protocol.ORMultiMap.Entry();
                switch (e.Key)
                {
                    case int i:
                        thisEntry.IntKey = i;
                        break;
                    case long l:
                        thisEntry.LongKey = l;
                        break;
                    case string str:
                        thisEntry.StringKey = str;
                        break;
                    default:
                        thisEntry.OtherKey = _ser.OtherMessageToProto(e.Key);
                        break;
                }

                thisEntry.Value = ToProto(e.Value);
                entries.Add(thisEntry);
            }

            proto.Entries = entries;
        }

        private IORMultiValueDictionary ORMultiDictionaryFromLZ4Binary(in ReadOnlySpan<byte> bytes)
        {
            var ormm = LZ4MessagePackSerializer.Deserialize<Protocol.ORMultiMap>(bytes, s_defaultResolver);
            return ORMultiDictionaryFromProto(ormm);
        }

        private IORMultiValueDictionary ORMultiDictionaryFromProto(Protocol.ORMultiMap proto)
        {
            var keyType = GetTypeFromDescriptor(proto.Keys.TypeInfo);
            var valueType = GetTypeFromDescriptor(proto.ValueTypeInfo);

            var dictMaker = MultiDictMaker.MakeGenericMethod(keyType, valueType);
            return (IORMultiValueDictionary)dictMaker.Invoke(this, new object[] { proto });
        }

        private static readonly MethodInfo MultiDictMaker =
           typeof(ReplicatedDataSerializer).GetMethod(nameof(GenericORMultiDictionaryFromProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private IORMultiValueDictionary GenericORMultiDictionaryFromProto<TKey, TValue>(Protocol.ORMultiMap proto)
        {
            var keys = FromProto(proto.Keys);
            switch (proto.Keys.TypeInfo.Type)
            {
                case Protocol.ValType.Int:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => x.IntKey,
                            v => (ORSet<TValue>)FromProto(v.Value));
                        var orDict = new ORDictionary<int, ORSet<TValue>>((ORSet<int>)keys, entries);
                        return new ORMultiValueDictionary<int, TValue>(orDict, proto.WithValueDeltas);
                    }
                case Protocol.ValType.Long:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => x.LongKey,
                            v => (ORSet<TValue>)FromProto(v.Value));
                        var orDict = new ORDictionary<long, ORSet<TValue>>((ORSet<long>)keys, entries);
                        return new ORMultiValueDictionary<long, TValue>(orDict, proto.WithValueDeltas);
                    }
                case Protocol.ValType.String:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => x.StringKey,
                            v => (ORSet<TValue>)FromProto(v.Value));
                        var orDict = new ORDictionary<string, ORSet<TValue>>((ORSet<string>)keys, entries);
                        return new ORMultiValueDictionary<string, TValue>(orDict, proto.WithValueDeltas);
                    }
                default:
                    {
                        var entries = proto.Entries.ToImmutableDictionary(x => (TKey)_ser.OtherMessageFromProto(x.OtherKey),
                            v => (ORSet<TValue>)FromProto(v.Value));
                        var orDict = new ORDictionary<TKey, ORSet<TValue>>((ORSet<TKey>)keys, entries);
                        return new ORMultiValueDictionary<TKey, TValue>(orDict, proto.WithValueDeltas);
                    }
            }
        }

        private object ORMultiDictionaryDeltaFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = MessagePackSerializer.Deserialize<Protocol.ORMultiMapDelta>(bytes, s_defaultResolver);
            var orDictOp = ORDictionaryDeltaGroupFromProto(proto.Delta);

            var orSetType = orDictOp.ValueType.GenericTypeArguments[0];
            var maker = ORMultiDictionaryDeltaMaker.MakeGenericMethod(orDictOp.KeyType, orSetType);
            return (IORMultiValueDictionaryDeltaOperation)maker.Invoke(this, new object[] { orDictOp, proto.WithValueDeltas });
        }

        private static readonly MethodInfo ORMultiDictionaryDeltaMaker =
            typeof(ReplicatedDataSerializer).GetMethod(nameof(ORMultiDictionaryDeltaFromProto), BindingFlags.Instance | BindingFlags.NonPublic);

        private IORMultiValueDictionaryDeltaOperation ORMultiDictionaryDeltaFromProto<TKey, TValue>(ORDictionary.IDeltaOperation op, bool withValueDeltas)
        {
            var casted = (ORDictionary<TKey, ORSet<TValue>>.IDeltaOperation)op;
            return new ORMultiValueDictionary<TKey, TValue>.ORMultiValueDictionaryDelta(casted, withValueDeltas);
        }

        #endregion

        #region Keys

        private Protocol.Key ToProto(IKey key, out string manifest)
        {
            switch (key)
            {
                case IORSetKey orkey:
                    manifest = ORSetKeyManifest;
                    return new Protocol.Key(key.Id, Protocol.KeyType.ORSetKey, GetTypeDescriptor(orkey.SetType));
                case IGSetKey gSetKey:
                    manifest = GSetKeyManifest;
                    return new Protocol.Key(key.Id, Protocol.KeyType.GSetKey, GetTypeDescriptor(gSetKey.SetType));
                case GCounterKey _:
                    manifest = GCounterKeyManifest;
                    return new Protocol.Key(key.Id, Protocol.KeyType.GCounterKey);
                case PNCounterKey _:
                    manifest = PNCounterKeyManifest;
                    return new Protocol.Key(key.Id, Protocol.KeyType.PNCounterKey);
                case FlagKey _:
                    manifest = FlagKeyManifest;
                    return new Protocol.Key(key.Id, Protocol.KeyType.FlagKey);
                case ILWWRegisterKey registerKey:
                    manifest = LWWRegisterKeyManifest;
                    return new Protocol.Key(key.Id, Protocol.KeyType.LWWRegisterKey, GetTypeDescriptor(registerKey.RegisterType));
                case IORDictionaryKey dictionaryKey:
                    manifest = ORMapKeyManifest;
                    return new Protocol.Key(key.Id, Protocol.KeyType.ORMapKey, GetTypeDescriptor(dictionaryKey.KeyType), GetTypeDescriptor(dictionaryKey.ValueType));
                case ILWWDictionaryKey lwwDictKey:
                    manifest = LWWMapKeyManifest;
                    return new Protocol.Key(key.Id, Protocol.KeyType.LWWMapKey, GetTypeDescriptor(lwwDictKey.KeyType), GetTypeDescriptor(lwwDictKey.ValueType));
                case IPNCounterDictionaryKey pnDictKey:
                    manifest = PNCounterMapKeyManifest;
                    return new Protocol.Key(key.Id, Protocol.KeyType.PNCounterMapKey, GetTypeDescriptor(pnDictKey.KeyType));
                case IORMultiValueDictionaryKey orMultiKey:
                    manifest = ORMultiMapKeyManifest;
                    return new Protocol.Key(key.Id, Protocol.KeyType.ORMultiMapKey, GetTypeDescriptor(orMultiKey.KeyType), GetTypeDescriptor(orMultiKey.ValueType));
                default:
                    throw GetUnrecognizedKeyTypeException(key);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static SerializationException GetUnrecognizedKeyTypeException(IKey key)
        {
            return new SerializationException($"Unrecognized key type [{key}]");
        }

        [MethodImpl(InlineOptions.AggressiveOptimization)]
        private Protocol.Key KeyFromBinary(in ReadOnlySpan<byte> bytes)
        {
            return MessagePackSerializer.Deserialize<Protocol.Key>(bytes, s_defaultResolver);
        }

        private IKey ORSetKeyFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = KeyFromBinary(bytes);
            var keyType = GetTypeFromDescriptor(proto.KeyTypeInfo);
            var genType = typeof(ORSetKey<>).GetCachedGenericType(keyType);
            return (IKey)Activator.CreateInstance(genType, proto.KeyId);
        }

        private IKey GSetKeyFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = KeyFromBinary(bytes);
            var keyType = GetTypeFromDescriptor(proto.KeyTypeInfo);
            var genType = typeof(GSetKey<>).GetCachedGenericType(keyType);
            return (IKey)Activator.CreateInstance(genType, proto.KeyId);
        }

        private IKey LWWRegisterKeyFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = KeyFromBinary(bytes);
            var keyType = GetTypeFromDescriptor(proto.KeyTypeInfo);
            var genType = typeof(LWWRegisterKey<>).GetCachedGenericType(keyType);
            return (IKey)Activator.CreateInstance(genType, proto.KeyId);
        }

        private IKey GCounterKeyFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = KeyFromBinary(bytes);
            return new GCounterKey(proto.KeyId);
        }

        private IKey PNCounterKeyFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = KeyFromBinary(bytes);
            return new PNCounterKey(proto.KeyId);
        }

        private IKey FlagKeyFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = KeyFromBinary(bytes);
            return new FlagKey(proto.KeyId);
        }

        private IKey ORDictionaryKeyFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = KeyFromBinary(bytes);
            var keyType = GetTypeFromDescriptor(proto.KeyTypeInfo);
            var valueType = GetTypeFromDescriptor(proto.ValueTypeInfo);

            var genType = typeof(ORDictionaryKey<,>).GetCachedGenericType(keyType, valueType);
            return (IKey)Activator.CreateInstance(genType, proto.KeyId);
        }

        private IKey LWWDictionaryKeyFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = KeyFromBinary(bytes);
            var keyType = GetTypeFromDescriptor(proto.KeyTypeInfo);
            var valueType = GetTypeFromDescriptor(proto.ValueTypeInfo);

            var genType = typeof(LWWDictionaryKey<,>).GetCachedGenericType(keyType, valueType);
            return (IKey)Activator.CreateInstance(genType, proto.KeyId);
        }

        private IKey PNCounterDictionaryKeyFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = KeyFromBinary(bytes);
            var keyType = GetTypeFromDescriptor(proto.KeyTypeInfo);

            var genType = typeof(PNCounterDictionaryKey<>).GetCachedGenericType(keyType);
            return (IKey)Activator.CreateInstance(genType, proto.KeyId);
        }

        private IKey ORMultiValueDictionaryKeyFromBinary(in ReadOnlySpan<byte> bytes)
        {
            var proto = KeyFromBinary(bytes);
            var keyType = GetTypeFromDescriptor(proto.KeyTypeInfo);
            var valueType = GetTypeFromDescriptor(proto.ValueTypeInfo);

            var genType = typeof(ORMultiValueDictionaryKey<,>).GetCachedGenericType(keyType, valueType);
            return (IKey)Activator.CreateInstance(genType, proto.KeyId);
        }

        #endregion
    }
}