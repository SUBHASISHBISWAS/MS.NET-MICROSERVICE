using CQRS.CORE.DOMAIN;
using CQRS.CORE.HANDLERS;
using CQRS.CORE.INFRASTRUCTURE;
using POST.CMD.DOMAIN.AGGREGATES;

namespace POST.CMD.INFRASTRUCTURE.HANDLERS
{
    public class EventSourcingHandler : IEventSourcingHandler<PostAggregate>
    {
        private readonly IEventStore _eventStore;

        public EventSourcingHandler(IEventStore eventStoreRepository)
        {
            _eventStore = eventStoreRepository;
        }

        public async Task SaveAsync(AggregateRoot aggregate)
        {
            await _eventStore.SaveEventsAsync(aggregate.Id, aggregate.GetUncommittedChanges(), aggregate.Version);
            aggregate.MarkChangesAsCommitted();
        }

        public async Task<PostAggregate> GetByIdAsync(Guid aggregateId)
        {
            var aggregate = new PostAggregate();
            var events = await _eventStore.GetEventsAsync(aggregateId);

            if (events == null || !events.Any()) return aggregate;
            aggregate.ReplayEvent(events);
            aggregate.Version = events.Select(x => x.Version).Max();
            return aggregate;
        }
    }
}
