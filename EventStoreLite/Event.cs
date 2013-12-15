using System;

namespace EventStoreLite
{
    /// <summary>
    /// Event base class type.
    /// </summary>
    public abstract class Event : IDomainEvent
    {
        /// <summary>
        /// Gets the event time stamp.
        /// </summary>
        public DateTimeOffset TimeStamp { get; private set; }

        /// <summary>
        /// Gets the change sequence.
        /// </summary>
        public int ChangeSequence { get; private set; }

        internal void SetTimeStamp(DateTimeOffset dateTimeOffset)
        {
            TimeStamp = dateTimeOffset;
        }

        internal void SetChangeSequence(int changeSequence)
        {
            ChangeSequence = changeSequence;
        }
    }
}