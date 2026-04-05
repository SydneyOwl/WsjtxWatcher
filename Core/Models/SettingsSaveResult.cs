namespace WsjtxWatcher.Core.Models;

public sealed record SettingsSaveResult(bool RestartRequired, bool ServiceWasRunning)
{
    public bool ServiceRestarted => RestartRequired && ServiceWasRunning;

    public bool ManualStartRecommended => RestartRequired && !ServiceWasRunning;
}
