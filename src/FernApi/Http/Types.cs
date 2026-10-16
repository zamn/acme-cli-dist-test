namespace FernApi.Http;

/// <summary>
/// Defines a mapping between an HTTP error response (status code and content type) and how it should be handled.
/// </summary>
public class ErrorMapping
{
    public int StatusCode { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public global::System.Type TargetType;
    private global::System.Type? _exceptionType;
    public global::System.Type ExceptionType
    {
        // Check that the type extends Exception
        get => _exceptionType!;
        set
        {
            if (value != null && !typeof(Exception).IsAssignableFrom(value))
            {
                throw new ArgumentException(
                    $"Type {value.Name} must extend Exception",
                    nameof(ExceptionType)
                );
            }
            _exceptionType = value;
        }
    }
}
