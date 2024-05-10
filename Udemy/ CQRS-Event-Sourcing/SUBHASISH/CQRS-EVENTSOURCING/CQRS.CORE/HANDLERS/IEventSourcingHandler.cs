using CQRS.CORE.DOMAIN;

namespace CQRS.CORE.HANDLERS
{
    public interface IEventSourcingHandler<T>
    {
        Task SaveAsync(AggregateRoot aggregate);

        Task<T> GetByIdAsync(Guid aggregateId);
    }
}
