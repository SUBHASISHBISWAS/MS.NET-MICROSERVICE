using CQRS.CORE.EVENTS;

namespace CQRS.CORE.DOMAIN
{
    public interface IEventStoreRepository
    {
        Task SaveAsync(EventModel @event);
        Task<List<EventModel>> FindByAggregateId(Guid aggregateId);
    }
}
