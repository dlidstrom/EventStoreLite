namespace EventStoreLite
{
    using System;

    public class EventNotRegisteredException : Exception
    {
        public EventNotRegisteredException(Type getType)
            : base(string.Format("No handler for {0}", getType))
        {
        }
    }
}