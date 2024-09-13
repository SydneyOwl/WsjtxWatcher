using System.Net;
using Android.Util;
using WsjtxUtils.WsjtxMessages.Messages;
using WsjtxUtils.WsjtxUdpServer;
using WsjtxWatcher.ViewModels;
using Exception = Java.Lang.Exception;

namespace WsjtxWatcher.Utils.UdpServer;

public class UdpServerConf
{
    public IWsjtxUdpMessageHandler handler;
    public string ip;
    public string port;
}

public sealed class UdpServer
{
    private static string TAG = "UdpServer";

    private static UdpServer instance;

    private UdpServerConf conf;

    private WsjtxUdpServer server;

    private CancellationTokenSource tokenSource;

    public static UdpServer getInstance()
    {
        if (instance == null) instance = new UdpServer();
        return instance;
    }

    public bool isServiceRunning()
    {
        if (server == null) return false;
        return server.IsRunning;
    }

    public Task startServer(UdpServerConf conf)
    {
        this.conf = conf;
        try
        {
            stopServer();
        }
        catch
        {
            //ignored
        }

        tokenSource = new CancellationTokenSource();
        IPAddress ip;
        int port;
        if (!IPAddress.TryParse(conf.ip, out ip)) throw new Exception($"Failed to parse ip {conf.ip}!");
        if (!int.TryParse(conf.port, out port)) throw new Exception($"Failed to parse port {conf.port}!");
        server = new WsjtxUdpServer(conf.handler, ip, port);
        Log.Debug("Server",
            $"Starting UDP server: {server.LocalEndpoint.Address}:{server.LocalEndpoint.Port} IsMulticast:{server.IsMulticast} {(server.IsMulticast ? ip : string.Empty)}");
        return Task.Run(() =>
        {
            try
            {
                server.Start(tokenSource);
            }
            catch (Exception e)
            {
                
            }
        });
    }

    // block!
    public void stopServer()
    {
        try
        {
            tokenSource?.Cancel();
            if (isServiceRunning())
            {
                Log.Debug("Server", "Service running, Try stopping...");
                server?.Stop();
                server?.Dispose();
            }
        }
        catch
        {
            //ignored...
        }
        
    }

    public void SendHaltTxMessage()
    {
        try
        {
            server.SendMessageTo(MainViewModel.GetInstance().sessionEndPoint, new HaltTx(MainViewModel.GetInstance().clientId));
            server.SendMessageTo(MainViewModel.GetInstance().sessionEndPoint, new HaltTx(MainViewModel.GetInstance().clientId, true));
        }
        catch
        {
            //ignored
        }
    }
    
    public void SendReplayMessage()
    {
        try
        {
            server.SendMessageTo(MainViewModel.GetInstance().sessionEndPoint, new Replay(MainViewModel.GetInstance().clientId));
        }
        catch
        {
            //ignored
        }
    }
    public void SendClearMessage()
    {
        try
        {
            server.SendMessageTo(MainViewModel.GetInstance().sessionEndPoint, new Clear(MainViewModel.GetInstance().clientId,ClearWindow.Both));
            server.SendMessageTo(MainViewModel.GetInstance().sessionEndPoint, new Clear(MainViewModel.GetInstance().clientId,ClearWindow.BandActivity));
            server.SendMessageTo(MainViewModel.GetInstance().sessionEndPoint, new Clear(MainViewModel.GetInstance().clientId,ClearWindow.RxFrequency));
        }
        catch
        {
            //ignored
        }
    }
}