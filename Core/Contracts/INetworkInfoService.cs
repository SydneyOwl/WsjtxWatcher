namespace WsjtxWatcher.Core.Contracts;

public interface INetworkInfoService
{
    bool IsWifiConnected();
    string GetLocalIpAddress();
}
