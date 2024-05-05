using CQRS.CORE.COMMANDS;

namespace POST.CMD.API.COMMANDS
{
    public class NewPostCommand : BaseCommand
    {
        public string Author { get; set; }
        public string Message { get; set; }
    }
}
