namespace FernApi.Core;

public interface IIsRetryableContent
{
    public bool IsRetryable { get; }
}
