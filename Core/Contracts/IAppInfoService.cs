namespace WsjtxWatcher.Core.Contracts;

public interface IAppInfoService
{
    string VersionName { get; }
    string AppDataDirectory { get; }
    string LogFilePath { get; }
}
