using CQRS.CORE.COMMANDS;

namespace POST.CMD.API.COMMANDS
{
    public class DeletePostCommand : BaseCommand
    {
        public string Username { get; set; }
    }
}
