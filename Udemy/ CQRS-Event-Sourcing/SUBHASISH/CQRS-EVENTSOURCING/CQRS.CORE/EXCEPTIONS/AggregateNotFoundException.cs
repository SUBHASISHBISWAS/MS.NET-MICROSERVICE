namespace CQRS.CORE.EXCEPTIONS
{
    public class AggregateNotFoundException : Exception
    {
        public AggregateNotFoundException(string message) : base(message)
        {

        }
    }
}
