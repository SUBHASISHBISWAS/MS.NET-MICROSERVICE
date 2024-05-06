using CQRS.CORE.EVENTS;

namespace POST.COMMON.EVENTS
{
    public class PostRemovedEvent : BaseEvent
    {
        public PostRemovedEvent() : base(nameof(PostRemovedEvent))
        {
        }
    }
}
