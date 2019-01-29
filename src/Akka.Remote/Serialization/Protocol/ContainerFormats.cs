using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Akka.Serialization.Protocol;
using MessagePack;

namespace Akka.Remote.Serialization.Protocol
{
    #region -- Common types --

    /// <summary>Defines a remote ActorRef that "remembers" and uses its original Actor instance on the original node.</summary>
    [MessagePackObject]
    public readonly struct ReadOnlyActorRefData
    {
        [Key(0)]
        public readonly string Path;

        [SerializationConstructor]
        public ReadOnlyActorRefData(string path) => Path = path;
    }
    [MessagePackObject]
    public sealed class ActorRefData
    {
        [Key(0)]
        public readonly string Path;

        [SerializationConstructor]
        public ActorRefData(string path) => Path = path;
    }

    /// <summary>Defines a remote address.</summary>
    [MessagePackObject]
    public readonly struct AddressData
    {
        [SerializationConstructor]
        public AddressData(string system, string hostname, uint port, string protocol)
        {
            System = system;
            Hostname = hostname;
            Port = port;
            Protocol = protocol;
        }

        [Key(0)]
        public readonly string System;

        [Key(1)]
        public readonly string Hostname;

        [Key(2)]
        public readonly uint Port;

        [Key(3)]
        public readonly string Protocol;
    }

    [MessagePackObject]
    public readonly struct Identify
    {
        [Key(0)]
        public readonly Payload MessageId;

        [SerializationConstructor]
        public Identify(Payload messageId) => MessageId = messageId;
    }

    [MessagePackObject]
    public readonly struct ActorIdentity
    {
        [Key(0)]
        public readonly Payload CorrelationId;

        [Key(1)]
        public readonly string Path;

        [SerializationConstructor]
        public ActorIdentity(Payload correlationId, string path)
        {
            CorrelationId = correlationId;
            Path = path;
        }
    }

    [MessagePackObject]
    public readonly struct RemoteWatcherHeartbeatResponse
    {
        [Key(0)]
        public readonly ulong Uid;

        [SerializationConstructor]
        public RemoteWatcherHeartbeatResponse(ulong uid) => Uid = uid;
    }

    [MessagePackObject]
    public sealed class ExceptionData
    {
        [Key(0)]
        public string TypeName { get; set; }

        [Key(1)]
        public string Message { get; set; }

        [Key(2)]
        public string StackTrace { get; set; }

        [Key(3)]
        public string Source { get; set; }

        [Key(4)]
        public ExceptionData InnerException { get; set; }

        [Key(5)]
        public Dictionary<string, Payload> CustomFields { get; set; }
    }

    #endregion

    #region -- ActorSelection related formats --

    [MessagePackObject]
    public readonly struct SelectionEnvelope
    {
        [SerializationConstructor]
        public SelectionEnvelope(Payload payload, List<Selection> pattern)
        {
            Payload = payload;
            Pattern = pattern;
        }

        [Key(0)]
        public readonly Payload Payload;

        [Key(1)]
        public readonly List<Selection> Pattern;
    }

    [MessagePackObject]
    public readonly struct Selection
    {
        public enum PatternType
        {
            NoPatern = 0,
            Parent = 1,
            ChildName = 2,
            ChildPattern = 3,
        }

        [SerializationConstructor]
        public Selection(PatternType type, string matcher)
        {
            Type = type;
            Matcher = matcher;
        }

        [Key(0)]
        public readonly PatternType Type;

        [Key(1)]
        public readonly string Matcher;
    }

    #endregion

    #region -- Google types --

    [MessagePackObject]
    public readonly struct Duration
    {
        [Key(0)]
        public readonly long Seconds;

        [Key(1)]
        public readonly int Nanos;

        [SerializationConstructor]
        public Duration(long seconds, int nanos)
        {
            Seconds = seconds;
            Nanos = nanos;
        }

        /// <summary>
        /// The number of nanoseconds in a second.
        /// </summary>
        public const int NanosecondsPerSecond = 1000000000;
        /// <summary>
        /// The number of nanoseconds in a BCL tick (as used by <see cref="TimeSpan"/> and <see cref="DateTime"/>).
        /// </summary>
        public const int NanosecondsPerTick = 100;

        /// <summary>
        /// The maximum permitted number of seconds.
        /// </summary>
        public const long MaxSeconds = 315576000000L;

        /// <summary>
        /// The minimum permitted number of seconds.
        /// </summary>
        public const long MinSeconds = -315576000000L;

        internal const int MaxNanoseconds = NanosecondsPerSecond - 1;
        internal const int MinNanoseconds = -NanosecondsPerSecond + 1;

        /// <summary>
        /// Converts this <see cref="Duration"/> to a <see cref="TimeSpan"/>.
        /// </summary>
        /// <remarks>If the duration is not a precise number of ticks, it is truncated towards 0.</remarks>
        /// <returns>The value of this duration, as a <c>TimeSpan</c>.</returns>
        /// <exception cref="InvalidOperationException">This value isn't a valid normalized duration, as
        /// described in the documentation.</exception>
        public TimeSpan ToTimeSpan()
        {
            checked
            {
                if (!IsNormalized(Seconds, Nanos))
                {
                    ThrowInvalidOperationExceptionn();
                }
                long ticks = Seconds * TimeSpan.TicksPerSecond + Nanos / NanosecondsPerTick;
                return TimeSpan.FromTicks(ticks);
            }
        }

        internal static bool IsNormalized(long seconds, int nanoseconds)
        {
            // Simple boundaries
            if (seconds < MinSeconds || seconds > MaxSeconds ||
                nanoseconds < MinNanoseconds || nanoseconds > MaxNanoseconds)
            {
                return false;
            }
            // We only have a problem is one is strictly negative and the other is
            // strictly positive.
            return Math.Sign(seconds) * Math.Sign(nanoseconds) != -1;
        }

        /// <summary>
        /// Converts the given <see cref="TimeSpan"/> to a <see cref="Duration"/>.
        /// </summary>
        /// <param name="timeSpan">The <c>TimeSpan</c> to convert.</param>
        /// <returns>The value of the given <c>TimeSpan</c>, as a <c>Duration</c>.</returns>
        public static Duration FromTimeSpan(TimeSpan timeSpan)
        {
            checked
            {
                long ticks = timeSpan.Ticks;
                long seconds = ticks / TimeSpan.TicksPerSecond;
                int nanos = (int)(ticks % TimeSpan.TicksPerSecond) * NanosecondsPerTick;
                return new Duration(seconds, nanos);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowInvalidOperationExceptionn()
        {
            throw GetException();
            InvalidOperationException GetException()
            {
                return new InvalidOperationException("Duration was not a valid normalized duration");
            }
        }
    }

    #endregion
}
