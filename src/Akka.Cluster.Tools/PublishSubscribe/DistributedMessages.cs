﻿//-----------------------------------------------------------------------
// <copyright file="DistributedMessages.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Immutable;
using System.Linq;
using Akka.Actor;
using Akka.Event;
using MessagePack;

namespace Akka.Cluster.Tools.PublishSubscribe
{
    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    public sealed class Put : IEquatable<Put>
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public IActorRef Ref { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="ref">TBD</param>
        [SerializationConstructor]
        public Put(IActorRef @ref)
        {
            Ref = @ref;
        }

        /// <inheritdoc/>
        public bool Equals(Put other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Equals(Ref, other.Ref);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Put);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Ref is object ? Ref.GetHashCode() : 0);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Put<ref:{Ref}>";
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    public sealed class Remove : IEquatable<Remove>
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public string Path { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="path">TBD</param>
        [SerializationConstructor]
        public Remove(string path)
        {
            Path = path;
        }

        /// <inheritdoc/>
        public bool Equals(Remove other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Equals(Path, other.Path);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Remove);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Path is object ? Path.GetHashCode() : 0);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Remove<path:{Path}>";
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    public sealed class Subscribe : IEquatable<Subscribe>
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public string Topic { get; }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public IActorRef Ref { get; }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(2)]
        public string Group { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="topic">TBD</param>
        /// <param name="ref">TBD</param>
        /// <param name="group">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="topic"/> is undefined.
        /// </exception>
        [SerializationConstructor]
        public Subscribe(string topic, IActorRef @ref, string @group = null)
        {
            if (string.IsNullOrEmpty(topic)) ThrowHelper.ThrowArgumentException_TopicMustBeDefined();

            Topic = topic;
            Group = @group;
            Ref = @ref;
        }

        /// <inheritdoc/>
        public bool Equals(Subscribe other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Equals(Topic, other.Topic) &&
                   Equals(Group, other.Group) &&
                   Equals(Ref, other.Ref);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Subscribe);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Topic is object ? Topic.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Group is object ? Group.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Ref is object ? Ref.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Subscribe<topic:{Topic}, group:{Group}, ref:{Ref}>";
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    public sealed class Unsubscribe : IEquatable<Unsubscribe>
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public string Topic { get; }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(1)]
        public IActorRef Ref { get; }

        /// <summary>
        /// TBD
        /// </summary>
        [Key(2)]
        public string Group { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="topic">TBD</param>
        /// <param name="ref">TBD</param>
        /// <param name="group">TBD</param>
        /// <exception cref="ArgumentException">
        /// This exception is thrown when the specified <paramref name="topic"/> is undefined.
        /// </exception>
        [SerializationConstructor]
        public Unsubscribe(string topic, IActorRef @ref, string @group = null)
        {
            if (string.IsNullOrEmpty(topic)) ThrowHelper.ThrowArgumentException_TopicMustBeDefined();

            Topic = topic;
            Group = @group;
            Ref = @ref;
        }

        /// <inheritdoc/>
        public bool Equals(Unsubscribe other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Equals(Topic, other.Topic) &&
                   Equals(Group, other.Group) &&
                   Equals(Ref, other.Ref);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Unsubscribe);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Topic is object ? Topic.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Group is object ? Group.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Ref is object ? Ref.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Unsubscribe<topic:{Topic}, group:{Group}, ref:{Ref}>";
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    public sealed class SubscribeAck : IEquatable<SubscribeAck>, IDeadLetterSuppression
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public Subscribe Subscribe { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="subscribe">TBD</param>
        /// <returns>TBD</returns>
        [SerializationConstructor]
        public SubscribeAck(Subscribe subscribe)
        {
            Subscribe = subscribe;
        }

        /// <inheritdoc/>
        public bool Equals(SubscribeAck other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Equals(Subscribe, other.Subscribe);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as SubscribeAck);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Subscribe is object ? Subscribe.GetHashCode() : 0);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"SubscribeAck<{Subscribe}>";
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    public sealed class UnsubscribeAck : IEquatable<UnsubscribeAck>
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public Unsubscribe Unsubscribe { get; }

        /// <summary>
        /// TBD
        /// </summary>
        [SerializationConstructor]
        public UnsubscribeAck(Unsubscribe unsubscribe)
        {
            Unsubscribe = unsubscribe;
        }

        /// <inheritdoc/>
        public bool Equals(UnsubscribeAck other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Equals(Unsubscribe, other.Unsubscribe);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as UnsubscribeAck);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Unsubscribe is object ? Unsubscribe.GetHashCode() : 0);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"UnsubscribeAck<{Unsubscribe}>";
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    public sealed class Publish : IDistributedPubSubMessage, IEquatable<Publish>
    {
        /// <summary>
        /// TBD
        /// </summary>
        public string Topic { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public object Message { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public bool SendOneMessageToEachGroup { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="topic">TBD</param>
        /// <param name="message">TBD</param>
        /// <param name="sendOneMessageToEachGroup">TBD</param>
        public Publish(string topic, object message, bool sendOneMessageToEachGroup = false)
        {
            Topic = topic;
            Message = message;
            SendOneMessageToEachGroup = sendOneMessageToEachGroup;
        }

        /// <inheritdoc/>
        public bool Equals(Publish other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Equals(Topic, other.Topic) &&
                   Equals(SendOneMessageToEachGroup, other.SendOneMessageToEachGroup) &&
                   Equals(Message, other.Message);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Publish);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Topic is object ? Topic.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Message is object ? Message.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ SendOneMessageToEachGroup.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Publish<topic:{Topic}, sendOneToEachGroup:{SendOneMessageToEachGroup}, message:{Message}>";
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    public sealed class Send : IDistributedPubSubMessage, IEquatable<Send>
    {
        /// <summary>
        /// TBD
        /// </summary>
        public string Path { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public object Message { get; }
        /// <summary>
        /// TBD
        /// </summary>
        public bool LocalAffinity { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="path">TBD</param>
        /// <param name="message">TBD</param>
        /// <param name="localAffinity">TBD</param>
        public Send(string path, object message, bool localAffinity = false)
        {
            Path = path;
            Message = message;
            LocalAffinity = localAffinity;
        }

        /// <inheritdoc/>
        public bool Equals(Send other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Equals(Path, other.Path) &&
                   Equals(LocalAffinity, other.LocalAffinity) &&
                   Equals(Message, other.Message);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as Send);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Path is object ? Path.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Message is object ? Message.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ LocalAffinity.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Send<path:{Path}, localAffinity:{LocalAffinity}, message:{Message}>";
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    public sealed class SendToAll : IDistributedPubSubMessage, IEquatable<SendToAll>
    {
        /// <summary>
        /// TBD
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public object Message { get; }

        /// <summary>
        /// TBD
        /// </summary>
        public bool ExcludeSelf { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="path">TBD</param>
        /// <param name="message">TBD</param>
        /// <param name="excludeSelf">TBD</param>
        public SendToAll(string path, object message, bool excludeSelf = false)
        {
            Path = path;
            Message = message;
            ExcludeSelf = excludeSelf;
        }

        /// <inheritdoc/>
        public bool Equals(SendToAll other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;
            return Equals(ExcludeSelf, other.ExcludeSelf) &&
                   Equals(Path, other.Path) &&
                   Equals(Message, other.Message);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as SendToAll);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Path is object ? Path.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Message is object ? Message.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ExcludeSelf.GetHashCode();
                return hashCode;
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"SendToAll<path:{Path}, excludeSelf:{ExcludeSelf}, message:{Message}>";
        }
    }

    /// <summary>
    /// TBD
    /// </summary>
    public sealed class GetTopics : ISingletonMessage
    {
        /// <summary>
        /// TBD
        /// </summary>
        public static readonly GetTopics Instance = new GetTopics();
        private GetTopics() { }
    }

    /// <summary>
    /// TBD
    /// </summary>
    [MessagePackObject]
    public sealed class CurrentTopics : IEquatable<CurrentTopics>
    {
        /// <summary>
        /// TBD
        /// </summary>
        [Key(0)]
        public IImmutableSet<string> Topics { get; }

        /// <summary>
        /// TBD
        /// </summary>
        /// <param name="topics">TBD</param>
        [SerializationConstructor]
        public CurrentTopics(IImmutableSet<string> topics)
        {
            Topics = topics ?? ImmutableHashSet<string>.Empty;
        }

        /// <inheritdoc/>
        public bool Equals(CurrentTopics other)
        {
            if (other is null) return false;
            if (ReferenceEquals(other, this)) return true;

            return Topics.SequenceEqual(other.Topics);
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            return Equals(obj as CurrentTopics);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return (Topics is object ? Topics.GetHashCode() : 0);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"CurrentTopics<{string.Join(",", Topics)}>";
        }
    }
}
