using CQRS.CORE.COMMANDS;

namespace CQRS.CORE.INFRASTRUCTURE
{
    /// <summary>
    ///   <br />
    /// </summary>
    public interface ICommandDispatcher
    {
        void RegisterHandler<T>(Func<T, Task> handler) where T : BaseCommand;
        Task SendAsync(BaseCommand command);
    }
}
