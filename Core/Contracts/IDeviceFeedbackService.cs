namespace WsjtxWatcher.Core.Contracts;

public interface IDeviceFeedbackService
{
    Task VibrateAsync(CancellationToken cancellationToken = default);
}
