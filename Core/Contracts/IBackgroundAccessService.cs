namespace WsjtxWatcher.Core.Contracts;

public interface IBackgroundAccessService
{
    bool IsIgnoringBatteryOptimizations();
    void RequestIgnoreBatteryOptimizations();
    void OpenBackgroundSettings();
}
