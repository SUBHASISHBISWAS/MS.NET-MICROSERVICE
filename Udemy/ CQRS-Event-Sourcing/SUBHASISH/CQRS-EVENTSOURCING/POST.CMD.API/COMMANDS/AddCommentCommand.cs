using CQRS.CORE.COMMANDS;

namespace POST.CMD.API.COMMANDS
{
    public class AddCommentCommand : BaseCommand
    {
        public string Comment { get; set; }

        public string Username { get; set; }
    }
}
