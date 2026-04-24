namespace Application.Exceptions;

public class SearchUnavailableException : Exception
{
    public SearchUnavailableException(string message) : base(message) {}
    
    public SearchUnavailableException(string message, Exception inner) : base(message, inner) { }
}