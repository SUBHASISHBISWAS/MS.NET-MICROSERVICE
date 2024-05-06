using CQRS.CORE.EVENTS;

namespace POST.COMMON.EVENTS
{
    public class MessageUpdatedEvent : BaseEvent
    {
        public MessageUpdatedEvent() : base(nameof(MessageUpdatedEvent))
        {
        }

        public string Message { get; set; }
    }
}
