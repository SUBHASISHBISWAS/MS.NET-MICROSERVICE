using CQRS.CORE.DOMAIN;
using CQRS.CORE.EVENTS;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using POST.CMD.INFRASTRUCTURE.CONFIG;

namespace POST.CMD.INFRASTRUCTURE.REPOSITORIES;

public class EventStoreRepository : IEventStoreRepository
{
    private readonly IMongoCollection<EventModel> _eventStoreCollection;


    public EventStoreRepository(IOptions<MongoDbConfig> config)
    {
        MongoClient client = new(config.Value.ConnectionString);
        IMongoDatabase database = client.GetDatabase(config.Value.Database);
        _eventStoreCollection = database.GetCollection<EventModel>(config.Value.Collection);
    }

    public async Task SaveAsync(EventModel @event)
    {
        await _eventStoreCollection.InsertOneAsync(@event).ConfigureAwait(false);
    }

    public async Task<List<EventModel>> FindByAggregateId(Guid aggregateId)
    {
        return await _eventStoreCollection.Find(evm => evm.AggregateIdentifier == aggregateId).ToListAsync()
            .ConfigureAwait(false);
    }
}