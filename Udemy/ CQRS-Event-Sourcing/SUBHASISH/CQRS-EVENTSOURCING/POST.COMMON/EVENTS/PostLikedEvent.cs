using CQRS.CORE.EVENTS;

namespace POST.COMMON.EVENTS
{
    public class PostLikedEvent : BaseEvent
    {
        public PostLikedEvent() : base(nameof(PostLikedEvent))
        {
        }
    }
}
