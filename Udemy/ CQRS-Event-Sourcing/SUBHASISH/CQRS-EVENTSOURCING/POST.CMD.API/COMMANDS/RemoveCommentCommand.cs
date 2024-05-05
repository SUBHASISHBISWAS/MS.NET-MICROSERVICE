using CQRS.CORE.COMMANDS;

namespace POST.CMD.API.COMMANDS
{
    public class RemoveCommentCommand:BaseCommand
    {
        public Guid CommentId { get; set; }

        public string Username { get; set; }
    }
}
