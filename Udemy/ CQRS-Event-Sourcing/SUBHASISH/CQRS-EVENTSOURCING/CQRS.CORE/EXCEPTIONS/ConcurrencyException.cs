namespace CQRS.CORE.EXCEPTIONS
{
    public class ConcurrencyException : Exception
    {
        public ConcurrencyException(string message) : base(message)
        {

        }
    }
}
