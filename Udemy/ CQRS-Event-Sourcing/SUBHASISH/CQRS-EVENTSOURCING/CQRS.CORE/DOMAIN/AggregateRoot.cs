using CQRS.CORE.EVENTS;
using System.Reflection;

namespace CQRS.CORE.DOMAIN
{
    public abstract class AggregateRoot
    {
        protected Guid _id;

        private readonly List<BaseEvent> _changes = new();

        public Guid Id => _id;

        public int Version { get; set; } = -1;


        public IEnumerable<BaseEvent> GetUncommittedChanges()
        {
            return _changes;
        }

        public void MarkChangesAsCommitted()
        {
            _changes.Clear();
        }

        private void ApplyChange(BaseEvent @event, bool isNew)
        {
            MethodInfo? method = this.GetType().GetMethod("Apply", new Type[] { @event.GetType() });

            if (method == null)
            {
                throw new ArgumentNullException(nameof(method), $"The Apply method was not found in the aggregate for {@event.GetType().Name}!");
            }

            method.Invoke(this, new object[] { @event });

            if (isNew)
            {
                _changes.Add(@event);
            }
        }

        protected void RaiseEvent(BaseEvent @event)
        {
            ApplyChange(@event, true);
        }

        /// <summary>Replays the event.</summary>
        /// <param name="events">The events.</param>
        public void ReplayEvent(IEnumerable<BaseEvent> events)
        {
            foreach (BaseEvent e in events)
            {
                ApplyChange(e, false);
            }
        }
    }
}
