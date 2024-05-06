using CQRS.CORE.COMMANDS;
using CQRS.CORE.INFRASTRUCTURE;

namespace POST.CMD.INFRASTRUCTURE.DISPATCHERS
{
    public class CommandDispatcher : ICommandDispatcher
    {
        private readonly Dictionary<Type, Func<BaseCommand, Task>> _handlers = new();

        public void RegisterHandler<T>(Func<T, Task> handler) where T : BaseCommand
        {
            if (_handlers.ContainsKey(typeof(T)))
            {
                throw new IndexOutOfRangeException("Can not register same Command Handler");
            }
            _handlers.Add(typeof(T), command => handler((T)command));
        }

        public async Task SendAsync(BaseCommand command)
        {
            if (_handlers.TryGetValue(command.GetType(), out Func<BaseCommand, Task> handler))
            {
                await handler(command);
            }
            else
            {
                throw new ArgumentException(nameof(handler), "No Command Handanler Registered");
            }

        }
    }
}
