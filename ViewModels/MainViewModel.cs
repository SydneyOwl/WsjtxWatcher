using System.Collections.ObjectModel;
using System.Net;
using _Microsoft.Android.Resource.Designer;
using WsjtxUtils.WsjtxUdpServer;
using WsjtxWatcher.Adapters;
using WsjtxWatcher.Ft8Transmit;
using WsjtxWatcher.Utils.UdpServer;

namespace WsjtxWatcher.ViewModels;

public class MainViewModel : ViewBase
{
    private static string TAG = "MainViewModel";

    private static MainViewModel instance;
    private bool _IsRecvTimeout;
    private bool _IsTransmitting;
    public bool LastTxStatus = false;

    // public static ReactiveProperty<bool> IsWaitingForConn { get; set; } = new(true); //是否连接中？
    //
    // public static ReactiveProperty<bool> IsTransmitting { get; set; } = new(false); //是否发射中？
    //
    // public static ReactiveProperty<string> TransmittingMessage { get; set;} = new(); //是否发射中？
    //
    // public static ReactiveProperty<bool> IsRecvTimeout { get; set;} = new(); //是否超时中？
    //
    // public static int TimeElapsed = 0;

    private bool _IsWaitingForConn = true;
    private string _TransmittingMessage = "";
    private bool _IsMsgServiceRunning = false;

    public Watchdog RecvWatchdog;

    public List<DecodedMsg> DecodedMsgList = new();

    public double currentFreq = 0;

    public EndPoint sessionEndPoint;
    
    public string clientId;

    public int aboutMe = 0;
    
    public int totalRecord = 0;

    public UdpServerConf udpConf;


    private MainViewModel()
    {
    }


    public bool IsWaitingForConn
    {
        get => _IsWaitingForConn;
        set
        {
            _IsWaitingForConn = value;
            OnPropertyChanged();
        }
    }
    
    public bool IsMsgServiceRunning
    {
        get => _IsMsgServiceRunning;
        set
        {
            _IsMsgServiceRunning = value;
            OnPropertyChanged();
        }
    }
    

    public bool IsTransmitting
    {
        get => _IsTransmitting;
        set
        {
            _IsTransmitting = value;
            OnPropertyChanged();
        }
    }

    public bool IsRecvTimeout
    {
        get => _IsRecvTimeout;
        set
        {
            _IsRecvTimeout = value;
            OnPropertyChanged();
        }
    }

    public string TransmittingMessage
    {
        get => _TransmittingMessage;
        set
        {
            _TransmittingMessage = value;
            OnPropertyChanged();
        }
    }

    public static MainViewModel GetInstance()
    {
        if (instance == null) instance = new MainViewModel();
        return instance;
    }
}