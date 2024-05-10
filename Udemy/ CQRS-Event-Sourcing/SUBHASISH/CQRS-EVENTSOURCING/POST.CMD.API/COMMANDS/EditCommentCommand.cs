using CQRS.CORE.COMMANDS;

namespace POST.CMD.API.COMMANDS
{
    public class EditCommentCommand : BaseCommand
    {
        public Guid CommentId { get; set; }
        public string Comment { get; set; }

        public string Username { get; set; }
    }
}
