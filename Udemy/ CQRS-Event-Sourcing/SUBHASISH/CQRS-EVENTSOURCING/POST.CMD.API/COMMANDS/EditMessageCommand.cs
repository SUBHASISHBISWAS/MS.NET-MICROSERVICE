using CQRS.CORE.COMMANDS;

namespace POST.CMD.API.COMMANDS
{
    public class EditMessageCommand : BaseCommand
    {
        public string Message { get; set; }
    }
}
