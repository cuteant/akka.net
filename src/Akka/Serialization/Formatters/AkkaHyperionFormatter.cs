using System;
using System.Linq.Expressions;
using Akka.Actor;
using Akka.Util;
using Hyperion;
using Hyperion.Surrogates;
using MessagePack.Formatters;

namespace Akka.Serialization.Formatters
{
    #region == ActorRefSurrogate ==

    public sealed class ActorRefSurrogate : StringPayloadSurrogate
    {
        private const string c_nobody = "nobody";

        public static ActorRefSurrogate ToSurrogate(IActorRef actorRef)
        {
            string path = null;
            if (actorRef != null)
            {
                if (actorRef is Nobody) // TODO: this is a hack. Should work without it
                {
                    path = c_nobody;
                }
                else
                {
                    path = Serialization.SerializedActorPath(actorRef);
                }
            }
            return new ActorRefSurrogate() { S = path };
        }

        public static IActorRef FromSurrogate(ActorRefSurrogate surrogate)
        {
            var path = surrogate.S;

            if (string.IsNullOrWhiteSpace(surrogate.S)) { return null; }

            if (string.Equals(c_nobody, path, StringComparison.Ordinal))
            {
                return Nobody.Instance;
            }

            var system = MsgPackSerializerHelper.LocalSystem.Value;
            if (system == null) { return default; }

            return system.Provider.ResolveActorRef(path);
        }
    }

    #endregion

    #region == AkkaHyperionExceptionFormatter ==

    internal class AkkaHyperionExceptionFormatter<TException> : HyperionExceptionFormatter<TException>
        where TException : Exception
    {
        public AkkaHyperionExceptionFormatter() : base(
            new SerializerOptions(
                versionTolerance: false,
                preserveObjectReferences: true,
                surrogates: new[]
                    {
                        Surrogate.Create<IActorRef, ActorRefSurrogate>(ActorRefSurrogate.ToSurrogate, ActorRefSurrogate.FromSurrogate),
                        Surrogate.Create<ISurrogated, ISurrogate>(
                            from => from.ToSurrogate(MsgPackSerializerHelper.LocalSystem.Value),
                            to => to.FromSurrogate(MsgPackSerializerHelper.LocalSystem.Value)),
                    }
                ))
        { }
    }

    #endregion

    #region == AkkaHyperionExpressionFormatter ==

    internal class AkkaHyperionExpressionFormatter<TExpression> : HyperionExpressionFormatter<TExpression>
        where TExpression : Expression
    {
        public AkkaHyperionExpressionFormatter() : base(
            new SerializerOptions(
                versionTolerance: false,
                preserveObjectReferences: true,
                surrogates: new[]
                    {
                        Surrogate.Create<IActorRef, ActorRefSurrogate>(ActorRefSurrogate.ToSurrogate, ActorRefSurrogate.FromSurrogate),
                        Surrogate.Create<ISurrogated, ISurrogate>(
                            from => from.ToSurrogate(MsgPackSerializerHelper.LocalSystem.Value),
                            to => to.FromSurrogate(MsgPackSerializerHelper.LocalSystem.Value)),
                    }
                ))
        { }
    }

    #endregion

    #region == AkkaHyperionFormatter ==

    internal class AkkaHyperionFormatter<T> : SimpleHyperionFormatter<T>
    {
        public AkkaHyperionFormatter() : base(
            new SerializerOptions(
                versionTolerance: false,
                preserveObjectReferences: true,
                surrogates: new[]
                    {
                        Surrogate.Create<IActorRef, ActorRefSurrogate>(ActorRefSurrogate.ToSurrogate, ActorRefSurrogate.FromSurrogate),
                        Surrogate.Create<ISurrogated, ISurrogate>(
                            from => from.ToSurrogate(MsgPackSerializerHelper.LocalSystem.Value),
                            to => to.FromSurrogate(MsgPackSerializerHelper.LocalSystem.Value)),
                    }
                ))
        { }
    }

    #endregion
}
