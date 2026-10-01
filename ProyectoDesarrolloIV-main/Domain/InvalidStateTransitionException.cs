namespace TodoApi.Domain
{
    public class InvalidStateTransitionException : Exception
    {
        public InvalidStateTransitionException(string message) : base(message) {
        }
    }
}
