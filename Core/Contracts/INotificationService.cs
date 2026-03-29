namespace WsjtxWatcher.Core.Contracts;

public interface INotificationService
{
    Task ShowMessageAlertAsync(string message, CancellationToken cancellationToken = default);
}
