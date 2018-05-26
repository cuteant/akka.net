//-----------------------------------------------------------------------
// <copyright file="Deadline.cs" company="Akka.NET Project">
//     Copyright (C) 2009-2018 Lightbend Inc. <http://www.lightbend.com>
//     Copyright (C) 2013-2018 .NET Foundation <https://github.com/akkadotnet/akka.net>
// </copyright>
//-----------------------------------------------------------------------

using System;
using MessagePack;

namespace Akka.Remote
{
    /// <summary>This class represents the latest date or time by which an operation should be completed.</summary>
    [MessagePackObject]
    public sealed class Deadline
    {
        /// <summary>Initializes a new instance of the <see cref="Deadline"/> class.</summary>
        /// <param name="when">The <see cref="DateTime"/> that the deadline is due.</param>
        [SerializationConstructor]
        public Deadline(DateTime when) => When = when;

        /// <summary>Determines whether the deadline has past.</summary>
        [IgnoreMember]
        public bool IsOverdue => DateTime.UtcNow > When;

        /// <summary>Determines whether there is still time left until the deadline.</summary>
        [IgnoreMember]
        public bool HasTimeLeft => DateTime.UtcNow < When;

        /// <summary>The <see cref="DateTime"/> that the deadline is due.</summary>
        [Key(0)]
        public readonly DateTime When;

        /// <summary><para>The amount of time left until the deadline is reached.</para>
        /// <note>Warning: creates a new <see cref="TimeSpan"/> instance each time it's used </note>.</summary>
        [IgnoreMember]
        public TimeSpan TimeLeft { get { return When - DateTime.UtcNow; } }

        #region Overrides

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj is Deadline deadlineObj)
            {
                return When.Equals(deadlineObj.When);
            }
            return false;
        }

        /// <inheritdoc/>
        public override int GetHashCode() => When.GetHashCode();

        #endregion

        #region Static members

        /// <summary>A deadline that is due <see cref="DateTime.UtcNow"/></summary>
        public static Deadline Now => new Deadline(DateTime.UtcNow);

        /// <summary>Adds a given <see cref="TimeSpan"/> to the due time of this <see cref="Deadline"/></summary>
        /// <param name="deadline">The deadline whose time is being extended</param>
        /// <param name="duration">The amount of time being added to the deadline</param>
        /// <returns>A new deadline with the specified duration added to the due time</returns>
        public static Deadline operator +(Deadline deadline, TimeSpan duration)
            => new Deadline(deadline.When.Add(duration));

        /// <summary>
        /// Adds a given <see cref="Nullable{TimeSpan}"/> to the due time of this <see cref="Deadline"/>
        /// </summary>
        /// <param name="deadline">The deadline whose time is being extended</param>
        /// <param name="duration">The amount of time being added to the deadline</param>
        /// <returns>A new deadline with the specified duration added to the due time</returns>
        public static Deadline operator +(Deadline deadline, TimeSpan? duration)
            => duration.HasValue ? new Deadline(deadline.When.Add(duration.Value)) : deadline;

        #endregion
    }
}