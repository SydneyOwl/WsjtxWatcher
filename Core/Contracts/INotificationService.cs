namespace WsjtxWatcher.Core.Contracts;

public interface INotificationService
{
    bool AreNotificationsEnabled();
    void OpenNotificationSettings();
    Task ShowMessageAlertAsync(string message, CancellationToken cancellationToken = default);
    Task ShowQsoLoggedAlertAsync(string callsign, string band, CancellationToken cancellationToken = default);
}
