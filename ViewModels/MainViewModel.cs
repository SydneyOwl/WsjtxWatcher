using System.Collections.ObjectModel;
using System.Net;
using WsjtxWatcher.Adapters;
using WsjtxWatcher.Ft8Transmit;
using WsjtxWatcher.Utils.UdpServer;

namespace WsjtxWatcher.ViewModels;

public class MainViewModel : ViewBase
{
    private static string _tag = "MainViewModel";

    private static MainViewModel _instance;
    private bool _isMsgServiceRunning;
    private bool _isRecvTimeout;
    private bool _isTransmitting;

    // public static ReactiveProperty<bool> IsWaitingForConn { get; set; } = new(true); //是否连接中？
    //
    // public static ReactiveProperty<bool> IsTransmitting { get; set; } = new(false); //是否发射中？
    //
    // public static ReactiveProperty<string> TransmittingMessage { get; set;} = new(); //是否发射中？
    //
    // public static ReactiveProperty<bool> IsRecvTimeout { get; set;} = new(); //是否超时中？
    //
    // public static int TimeElapsed = 0;

    private bool _isWaitingForConn = true;
    private string _transmittingMessage = "";

    public int AboutMe = 0;

    public string ClientId;

    public double CurrentFreq = 0;

    public ObservableCollection<DecodedMsg> DecodedMsgList = new();
    public bool LastTxStatus = false;
    public CallItemAdapter adapter;
    public Watchdog RecvWatchdog;

    public EndPoint SessionEndPoint;

    public int TotalRecord = 0;

    public UdpServerConf UdpConf;


    private MainViewModel()
    {
    }


    public bool IsWaitingForConn
    {
        get => _isWaitingForConn;
        set
        {
            _isWaitingForConn = value;
            OnPropertyChanged();
        }
    }

    public bool IsMsgServiceRunning
    {
        get => _isMsgServiceRunning;
        set
        {
            _isMsgServiceRunning = value;
            OnPropertyChanged();
        }
    }


    public bool IsTransmitting
    {
        get => _isTransmitting;
        set
        {
            _isTransmitting = value;
            OnPropertyChanged();
        }
    }

    public bool IsRecvTimeout
    {
        get => _isRecvTimeout;
        set
        {
            _isRecvTimeout = value;
            OnPropertyChanged();
        }
    }

    public string TransmittingMessage
    {
        get => _transmittingMessage;
        set
        {
            _transmittingMessage = value;
            OnPropertyChanged();
        }
    }

    public static MainViewModel GetInstance()
    {
        if (_instance == null) _instance = new MainViewModel();
        return _instance;
    }
}