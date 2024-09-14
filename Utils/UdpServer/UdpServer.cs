using System.Net;
using Android.Util;
using WsjtxUtils.WsjtxMessages.Messages;
using WsjtxUtils.WsjtxUdpServer;
using WsjtxWatcher.ViewModels;
using Exception = Java.Lang.Exception;

namespace WsjtxWatcher.Utils.UdpServer;

public class UdpServerConf
{
    public IWsjtxUdpMessageHandler Handler;
    public string Ip;
    public string Port;
}

public sealed class UdpServer
{
    private static string _tag = "UdpServer";

    private static UdpServer _instance;

    private UdpServerConf _conf;

    private WsjtxUdpServer _server;

    private CancellationTokenSource _tokenSource;

    public static UdpServer GetInstance()
    {
        if (_instance == null) _instance = new UdpServer();
        return _instance;
    }

    public bool IsServiceRunning()
    {
        if (_server == null) return false;
        return _server.IsRunning;
    }

    public Task StartServer(UdpServerConf conf)
    {
        this._conf = conf;
        try
        {
            StopServer();
        }
        catch
        {
            //ignored
        }

        _tokenSource = new CancellationTokenSource();
        IPAddress ip;
        int port;
        if (!IPAddress.TryParse(conf.Ip, out ip)) throw new Exception($"Failed to parse ip {conf.Ip}!");
        if (!int.TryParse(conf.Port, out port)) throw new Exception($"Failed to parse port {conf.Port}!");
        _server = new WsjtxUdpServer(conf.Handler, ip, port);
        Log.Debug("Server",
            $"Starting UDP server: {_server.LocalEndpoint.Address}:{_server.LocalEndpoint.Port} IsMulticast:{_server.IsMulticast} {(_server.IsMulticast ? ip : string.Empty)}");
        return Task.Run(() =>
        {
            try
            {
                _server.Start(_tokenSource);
            }
            catch (Exception e)
            {
            }
        });
    }

    // block!
    public void StopServer()
    {
        try
        {
            _tokenSource?.Cancel();
            if (IsServiceRunning())
            {
                Log.Debug("Server", "Service running, Try stopping...");
                _server?.Stop();
                _server?.Dispose();
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
            _server.SendMessageTo(MainViewModel.GetInstance().SessionEndPoint,
                new HaltTx(MainViewModel.GetInstance().ClientId));
            _server.SendMessageTo(MainViewModel.GetInstance().SessionEndPoint,
                new HaltTx(MainViewModel.GetInstance().ClientId, true));
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
            _server.SendMessageTo(MainViewModel.GetInstance().SessionEndPoint,
                new Replay(MainViewModel.GetInstance().ClientId));
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
            _server.SendMessageTo(MainViewModel.GetInstance().SessionEndPoint,
                new Clear(MainViewModel.GetInstance().ClientId, ClearWindow.Both));
            _server.SendMessageTo(MainViewModel.GetInstance().SessionEndPoint,
                new Clear(MainViewModel.GetInstance().ClientId, ClearWindow.BandActivity));
            _server.SendMessageTo(MainViewModel.GetInstance().SessionEndPoint,
                new Clear(MainViewModel.GetInstance().ClientId, ClearWindow.RxFrequency));
        }
        catch
        {
            //ignored
        }
    }
}