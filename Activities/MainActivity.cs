using _Microsoft.Android.Resource.Designer;
using Android;
using Android.Content;
using Android.Content.PM;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using WsjtxWatcher.Adapters;
using WsjtxWatcher.Behaviors.Watchers;
using WsjtxWatcher.Database;
using WsjtxWatcher.Ft8Transmit;
using WsjtxWatcher.Utils.AppPackage;
using WsjtxWatcher.Utils.DeviceActions;
using WsjtxWatcher.Utils.Network;
using WsjtxWatcher.Utils.UdpServer;
using WsjtxWatcher.Variables;
using WsjtxWatcher.ViewModels;

namespace WsjtxWatcher.Activities;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    public static string Tag = "MainActivity";
    public static Mutex Mutex = new();
    private readonly MainViewModel _model = MainViewModel.GetInstance();
    private TextView _aboutMe;
    private ListView _listView;
    private IMenuItem _startServer;
    private IMenuItem _stopServer;
    private TextView _totalRecord;
    private RelativeLayout _txLayout;
    private TextView _txmsg;

    protected override void OnCreate(Bundle savedInstanceState)
    {
        //test
        // CallsignFileOperation.getCountryNameToCN(this);
        // CallsignFileOperation.getCallsignInfo(this);

        base.OnCreate(savedInstanceState);
        SetContentView(ResourceConstant.Layout.activity_main);
        var resources = Application.Context.Resources;
        var config = resources.Configuration;
        var language = config.Locale.Language;
        SettingsVariables.CurrentLanguage = language; //zh
        //全屏
        // Window?.SetFlags(WindowManagerFlags.Fullscreen
        //     , WindowManagerFlags.Fullscreen);

        //禁止休眠
        Window?.SetFlags(WindowManagerFlags.KeepScreenOn
            , WindowManagerFlags.KeepScreenOn);

        OverrideSettings();
        new Task(() => { DatabaseHandler.GetInstance(this); }).Start();
        // Set our view from the "main" layout resource

        // 权限检查
        RequestPermissions(new[] { Manifest.Permission.Vibrate, Manifest.Permission.PostNotifications }, 645);

        if (CheckSelfPermission(Manifest.Permission.Vibrate) != Permission.Granted)
            Toast.MakeText(this, ResourceConstant.String.denied_vibrate, ToastLength.Short);

        if (CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted)
            Toast.MakeText(this, ResourceConstant.String.denied_notification, ToastLength.Short);

        _txmsg = FindViewById<TextView>(ResourceConstant.Id.transmittingMessageTextView);
        _txLayout = FindViewById<RelativeLayout>(ResourceConstant.Id.transmittingLayout);
        _listView = FindViewById<ListView>(ResourceConstant.Id.calllist_view);
        _aboutMe = FindViewById<TextView>(ResourceConstant.Id.about_me);
        _totalRecord = FindViewById<TextView>(ResourceConstant.Id.total_record);
        _model.adapter = new CallItemAdapter(this, _listView, _model.DecodedMsgList);
        _listView.Adapter = _model.adapter;
        // // // //设置发射消息框的动画
        _txLayout.Visibility = ViewStates.Visible;

        _model.DecodedMsgList.CollectionChanged += (sender, args) =>
        {
            RunOnUiThread(() =>
            {
                _model.adapter.NotifyDataSetChanged();
            });
        };

        // 超时指示
        _model.RecvWatchdog = new Watchdog(TimeSpan.FromSeconds(16),
            isTimeout =>
            {
                RunOnUiThread(() =>
                {
                    if (isTimeout)
                    {
                        _model.IsWaitingForConn = true;
                        SetTitle(GetString(ResourceConstant.String.recv_timeout));
                    }
                    else
                    {
                        _model.IsWaitingForConn = false;
                        SetTitle(GetString(ResourceConstant.String.app_name));
                    }
                });
            });

        _model.PropertyChanged += (sender, args) =>
        {
            switch (args.PropertyName)
            {
                case "IsTransmitting":
                    RunOnUiThread(() =>
                    {
                        SetTxLayoutConf(null, _model.IsTransmitting ? ViewStates.Visible : ViewStates.Invisible);
                        SetTitle(_model.IsTransmitting
                            ? GetString(ResourceConstant.String.app_name)
                            : GetString(ResourceConstant.String.receving));
                    });
                    break;
                case "TransmittingMessage":
                    Log.Debug(Tag, $"TransmittingMessage:{_model.TransmittingMessage}");
                    RunOnUiThread(() => { SetTxLayoutConf(_model.TransmittingMessage, ViewStates.Visible); });
                    break;
                case "IsWaitingForConn":
                    RunOnUiThread(() =>
                    {
                        if (_model.IsWaitingForConn)
                            SetTxLayoutConf(GetString(ResourceConstant.String.wait_conn), ViewStates.Visible);
                        else
                            SetTxLayoutConf(null, ViewStates.Invisible);
                    });
                    break;
                case "IsMsgServiceRunning":
                    RunOnUiThread(() =>
                    {
                        if (_model.IsMsgServiceRunning)
                        {
                            _startServer.SetEnabled(false);
                            _stopServer.SetEnabled(true);
                        }
                        else
                        {
                            _startServer.SetEnabled(true);
                            _stopServer.SetEnabled(false);
                            _model.RecvWatchdog.Stop();
                            SetTxLayoutConf(GetString(ResourceConstant.String.open_service), ViewStates.Visible);
                            SetTitle(GetString(ResourceConstant.String.app_name));
                        }
                    });
                    break;
            }
        };

        // 绑定三个按钮，真是太不优雅了，我都不敢想后面有多难维护
        FindViewById<Button>(ResourceConstant.Id.halt_tx).SetOnClickListener(new OnHaltTxListener());
        FindViewById<Button>(ResourceConstant.Id.replay).SetOnClickListener(new OnReplayListener());
        FindViewById<Button>(ResourceConstant.Id.clear).SetOnClickListener(new OnClearListener());

        _model.TransmittingMessage = GetString(ResourceConstant.String.open_service);
        var tgAni = AnimationUtils.LoadAnimation(this
            , ResourceConstant.Animation.view_blink);
        _txmsg.StartAnimation(tgAni);
        ChkInstallTime.SetCurrentTag(this);
    }

    public override bool OnCreateOptionsMenu(IMenu? menu)
    {
        MenuInflater.Inflate(ResourceConstant.Menu.main_menu, menu);
        _startServer = menu.FindItem(ResourceConstant.Id.start_server);
        _stopServer = menu.FindItem(ResourceConstant.Id.stop_server);
        _stopServer.SetEnabled(false);
        return true;
    }

    public override bool OnMenuItemSelected(int featureId, IMenuItem item)
    {
        switch (item.ItemId)
        {
            case ResourceConstant.Id.settings:
                var intent = new Intent(this, typeof(SettingsActivity));
                StartActivity(intent);
                break;
            case ResourceConstant.Id.start_server:
                var handler = new WsjtxMsgHandler();
                handler.OnDecodeMessageReceived += msg =>
                {
                    new Task(() =>
                    {
                        RunOnUiThread(() =>
                        {
                            if (!string.IsNullOrEmpty(SettingsVariables.MyCallsign) &&
                                msg.Message.Contains(SettingsVariables.MyCallsign))
                                _aboutMe.Text = $"与我有关：{++_model.AboutMe}";
                            _totalRecord.Text = $"总记录数：{++_model.TotalRecord}";
                        });
                    }).Start();
                };
                _model.UdpConf = new UdpServerConf
                {
                    Handler = handler,
                    Port = SettingsVariables.Port,
                    Ip = Wifi.GetLocalIpAddress(this)
                    // ip = IPAddress.Any.ToString()
                };
                var serviceIntent = new Intent(this, typeof(MsgPushService));
                StartService(serviceIntent);
                _model.RecvWatchdog.Start();
                Log.Debug(Tag, "Server started!");
                SetTitle(GetString(ResourceConstant.String.receving));
                SetTxLayoutConf(GetString(ResourceConstant.String.wait_conn), ViewStates.Visible);
                _startServer.SetEnabled(false);
                _stopServer.SetEnabled(true);
                break;
            case ResourceConstant.Id.stop_server:
                // ProgDialog prog = new ProgDialog(this);
                Log.Debug(Tag, "Server stopping");
                // prog.startAni();
                // UdpServer.getInstance().stopServer();
                var serviceIntent1 = new Intent(this, typeof(MsgPushService));
                StopService(serviceIntent1);
                Log.Debug(Tag, "Server stopped!");
                _model.RecvWatchdog.Stop();
                SetTitle(GetString(ResourceConstant.String.app_name));
                SetTxLayoutConf(GetString(ResourceConstant.String.open_service), ViewStates.Visible);
                // prog.stopAni();
                _startServer.SetEnabled(true);
                _stopServer.SetEnabled(false);
                break;
        }

        return base.OnOptionsItemSelected(item);
    }

    private void SetTxLayoutConf(string msg, ViewStates vs)
    {
        Mutex.WaitOne();
        if (!string.IsNullOrEmpty(msg)) _txmsg.Text = msg;
        _txLayout.Visibility = vs;
        Mutex.ReleaseMutex();
    }

    private void SetTitle(string msg)
    {
        Mutex.WaitOne();
        Title = msg;
        Mutex.ReleaseMutex();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        var serviceIntent1 = new Intent(this, typeof(MsgPushService));
        StopService(serviceIntent1);
    }

    // 读出设置
    private void OverrideSettings()
    {
        var sharedPref = GetSharedPreferences(GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        SettingsVariables.Port = sharedPref.GetString("port", "2237");
        SettingsVariables.MyCallsign = sharedPref.GetString("callsign", "");
        SettingsVariables.MyLocation = sharedPref.GetString("location", "");
        SettingsVariables.SendNotificationOnCall = sharedPref.GetBoolean("send_notification_on_call", false);
        SettingsVariables.SendNotificationOnAll = sharedPref.GetBoolean("send_notification_on_all", false);
        SettingsVariables.SendNotificationOnDxcc = sharedPref.GetBoolean("send_notification_on_dxcc", false);
        SettingsVariables.VibrateOnCall = sharedPref.GetBoolean("vibrate_on_call", false);
        SettingsVariables.VibrateOnAll = sharedPref.GetBoolean("vibrate_on_all", false);
        SettingsVariables.VibrateOnDxcc = sharedPref.GetBoolean("vibrate_on_dxcc", false);
    }
}