using CQRS.CORE.MESSAGES;

namespace CQRS.CORE.EVENTS
{
    public abstract class BaseEvent : Message
    {
        protected BaseEvent(string type)
        {
            Type = type;
        }

        public int Version { get; set; }

        public string Type { get; set; }
    }
}
