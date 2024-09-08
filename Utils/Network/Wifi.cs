using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Net;
using Android.Net.Wifi;

namespace WsjtxWatcher.Utils.Network;

public class Wifi
{
    public static bool isWificonnected(Context context)
    {
        var connectivityManager = (ConnectivityManager)context
            .GetSystemService(Context.ConnectivityService);
        var wifiNetworkInfo = connectivityManager
            .GetNetworkInfo(ConnectivityType.Wifi);
        return wifiNetworkInfo.IsConnected;
    }

    public static string getLocalIPAddress(Context context)
    {
        var wifiManager = (WifiManager)context.GetSystemService(Context.WifiService);
        if (wifiManager != null)
        {
            var wifiInfo = wifiManager.ConnectionInfo;
            var ipAddress = wifiInfo.IpAddress;
            return
                $"{ipAddress & 0xff}.{(ipAddress >> 8) & 0xff}.{(ipAddress >> 16) & 0xff}.{(ipAddress >> 24) & 0xff}";
        }

        return context.GetString(ResourceConstant.String.unknown_ip);
    }
}