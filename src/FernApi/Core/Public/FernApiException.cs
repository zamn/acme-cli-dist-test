namespace FernApi;

/// <summary>
/// Base exception class for all exceptions thrown by the SDK.
/// </summary>
public class FernApiException(string message, Exception? innerException = null)
    : Exception(message, innerException);
