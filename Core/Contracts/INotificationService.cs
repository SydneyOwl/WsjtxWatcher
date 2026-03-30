namespace WsjtxWatcher.Core.Contracts;

public interface INotificationService
{
    bool AreNotificationsEnabled();
    void OpenNotificationSettings();
    Task ShowMessageAlertAsync(string message, CancellationToken cancellationToken = default);
}
