using Android.App;
using Android.Content;
using Android.Net;
using WsjtxWatcher.Core.Contracts;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidNetworkInfoService : INetworkInfoService
{
    private readonly Application _application;

    public AndroidNetworkInfoService(Application application)
    {
        _application = application;
    }

    public bool IsWifiConnected()
    {
        var connectivityManager = (ConnectivityManager?)_application.GetSystemService(Context.ConnectivityService);
        var activeNetwork = connectivityManager?.ActiveNetwork;
        if (connectivityManager is null || activeNetwork is null)
        {
            return false;
        }

        var capabilities = connectivityManager.GetNetworkCapabilities(activeNetwork);
        return capabilities?.HasTransport(TransportType.Wifi) == true;
    }

    public string GetLocalIpAddress()
    {
        var connectivityManager = (ConnectivityManager?)_application.GetSystemService(Context.ConnectivityService);
        var activeNetwork = connectivityManager?.ActiveNetwork;
        if (connectivityManager is null || activeNetwork is null)
        {
            return _application.GetString(Resource.String.unknown_ip);
        }

        var linkProperties = connectivityManager.GetLinkProperties(activeNetwork);
        var hostAddress = linkProperties?.LinkAddresses?
            .Select(address => address?.Address?.HostAddress)
            .FirstOrDefault(address => !string.IsNullOrWhiteSpace(address) && !address.Contains(':'));

        return hostAddress ?? _application.GetString(Resource.String.unknown_ip);
    }
}
