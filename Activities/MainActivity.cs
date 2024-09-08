using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Views.Animations;
using WsjtxWatcher.Adapters;
using WsjtxWatcher.Behaviors.Watchers;
using WsjtxWatcher.Database;
using WsjtxWatcher.Dialogs;
using WsjtxWatcher.Ft8Transmit;
using WsjtxWatcher.Utils.Network;
using WsjtxWatcher.Utils.UdpServer;
using WsjtxWatcher.Utils.UTCTimer;
using WsjtxWatcher.Variables;
using WsjtxWatcher.ViewModels;

namespace WsjtxWatcher.Activities;

[Activity(Label = "@string/app_name", MainLauncher = true)]
public class MainActivity : Activity
{
    public static string TAG = "MainActivity";
    public static Mutex mutex = new();
    private readonly MainViewModel model = MainViewModel.GetInstance();
    private IMenuItem startServer;
    private IMenuItem stopServer;
    private RelativeLayout txLayout;
    private TextView txmsg;
    private ListView listView;
    private TextView aboutMe;
    private TextView totalRecord;
    private CallItemAdapter adapter;

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
        SettingsVariables.currentLanguage = language; //zh
        //全屏
        // Window?.SetFlags(WindowManagerFlags.Fullscreen
        //     , WindowManagerFlags.Fullscreen);

        //禁止休眠
        Window?.SetFlags(WindowManagerFlags.KeepScreenOn
            , WindowManagerFlags.KeepScreenOn);

        OverrideSettings();
        Task.Run(() => { DatabaseHandler.GetInstance(this); });
        // Set our view from the "main" layout resource

        txmsg = FindViewById<TextView>(ResourceConstant.Id.transmittingMessageTextView);
        txLayout = FindViewById<RelativeLayout>(ResourceConstant.Id.transmittingLayout);
        listView = FindViewById<ListView>(ResourceConstant.Id.calllist_view);
        aboutMe = FindViewById<TextView>(ResourceConstant.Id.about_me);
        totalRecord = FindViewById<TextView>(ResourceConstant.Id.total_record);
        adapter = new CallItemAdapter(this,ResourceConstant.Layout.call_item, model.DecodedMsgList);
        listView.Adapter = adapter;
        // // // //设置发射消息框的动画
        txLayout.Visibility = ViewStates.Visible;
        

        // 超时指示
        model.RecvWatchdog = new Watchdog(TimeSpan.FromSeconds(16),
            isTimeout =>
            {
                RunOnUiThread(() =>
                {
                    if (isTimeout)
                    {
                        model.IsWaitingForConn = true;
                        setTitle(GetString(ResourceConstant.String.recv_timeout));
                    }
                    else
                    {
                        model.IsWaitingForConn = false;
                        setTitle(GetString(ResourceConstant.String.app_name));
                    }
                });
            });

        model.PropertyChanged += (sender, args) =>
        {
            switch (args.PropertyName)
            {
                case "IsTransmitting":
                    RunOnUiThread(() =>
                    {
                        setTxLayoutConf(null, model.IsTransmitting ? ViewStates.Visible : ViewStates.Invisible);
                        setTitle(model.IsTransmitting
                            ? GetString(ResourceConstant.String.app_name)
                            : GetString(ResourceConstant.String.receving));
                    });
                    break;
                case "TransmittingMessage":
                    Log.Debug(TAG, $"TransmittingMessage:{model.TransmittingMessage}");
                    RunOnUiThread(() => { setTxLayoutConf(model.TransmittingMessage, ViewStates.Visible); });
                    break;
                case "IsWaitingForConn":
                    RunOnUiThread(() =>
                    {
                        if (model.IsWaitingForConn)
                            setTxLayoutConf(GetString(ResourceConstant.String.wait_conn), ViewStates.Visible);
                        else
                            setTxLayoutConf(null, ViewStates.Invisible);
                    });
                    break;
            }
        };
        
        // 绑定三个按钮，真是太不优雅了，我都不敢想后面有多难维护
        FindViewById<Button>(ResourceConstant.Id.halt_tx).SetOnClickListener(new OnHaltTxListener());
        FindViewById<Button>(ResourceConstant.Id.replay).SetOnClickListener(new OnReplayListener());
        FindViewById<Button>(ResourceConstant.Id.clear).SetOnClickListener(new OnClearListener(adapter));

        model.TransmittingMessage = GetString(ResourceConstant.String.open_service);
        var tgAni = AnimationUtils.LoadAnimation(this
            , ResourceConstant.Animation.view_blink);
        txmsg.StartAnimation(tgAni);
    }

    public override bool OnCreateOptionsMenu(IMenu? menu)
    {
        MenuInflater.Inflate(ResourceConstant.Menu.main_menu, menu);
        startServer = menu.FindItem(ResourceConstant.Id.start_server);
        stopServer = menu.FindItem(ResourceConstant.Id.stop_server);
        stopServer.SetEnabled(false);
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
                var handler = new WsjtxMsgHandler(this);
                handler.OnDecodeMessageReceived += message =>
                {
                    new Task(() =>
                    {
                        RunOnUiThread(() =>
                        {
                            var msg = DecodedMsg.RawDecodedToDecodedMsg(message);
                            adapter.Add(msg);
                            if (!string.IsNullOrEmpty(SettingsVariables.myCallsign) &&
                                msg.Message.Contains(SettingsVariables.myCallsign))
                            {
                                aboutMe.Text = $"与我有关：{++model.aboutMe}";
                            }

                            totalRecord.Text = $"总记录数：{++model.totalRecord}";
                        });
                    }).Start();
                };
                handler.OnStatusMessageReceived += message =>
                {
                    RunOnUiThread(() =>
                    {
                        if (!model.LastTxStatus && message.Transmitting)
                        {
                            adapter.Add(new DecodedMsg()
                            {
                                Transmitter = "USER_TRANSMIT",
                                Message = message.TXMessage
                            });
                        }

                        model.LastTxStatus = message.Transmitting;
                    });
                };
                UdpServer.getInstance().startServer(new UdpServerConf
                {
                    handler = handler,
                    port = SettingsVariables.port,
                    ip = Wifi.getLocalIPAddress(this)
                    // ip = IPAddress.Any.ToString()
                });
                model.RecvWatchdog.Start();
                Log.Debug(TAG, "Server started!");
                setTitle(GetString(ResourceConstant.String.receving));
                setTxLayoutConf(GetString(ResourceConstant.String.wait_conn), ViewStates.Visible);
                startServer.SetEnabled(false);
                stopServer.SetEnabled(true);
                break;
            case ResourceConstant.Id.stop_server:
                // ProgDialog prog = new ProgDialog(this);
                Log.Debug(TAG, "Server stopping");
                // prog.startAni();
                UdpServer.getInstance().stopServer();
                Log.Debug(TAG, "Server stopped!");
                model.RecvWatchdog.Stop();
                setTitle(GetString(ResourceConstant.String.app_name));
                setTxLayoutConf(GetString(ResourceConstant.String.open_service), ViewStates.Visible);
                // prog.stopAni();
                startServer.SetEnabled(true);
                stopServer.SetEnabled(false);
                break;
        }

        return base.OnOptionsItemSelected(item);
    }

    private void setTxLayoutConf(string msg, ViewStates vs)
    {
        mutex.WaitOne();
        if (!string.IsNullOrEmpty(msg)) txmsg.Text = msg;
        txLayout.Visibility = vs;
        mutex.ReleaseMutex();
    }

    private void setTitle(string msg)
    {
        mutex.WaitOne();
        Title = msg;
        mutex.ReleaseMutex();
    }


    // 读出设置
    private void OverrideSettings()
    {
        var sharedPref = GetSharedPreferences(GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        SettingsVariables.port = sharedPref.GetString("port", "2237");
        SettingsVariables.myCallsign = sharedPref.GetString("callsign", "");
        SettingsVariables.myLocation = sharedPref.GetString("location", "");
        SettingsVariables.send_notification_on_call = sharedPref.GetBoolean("send_notification_on_call", false);
        SettingsVariables.vibrate_on_call = sharedPref.GetBoolean("vibrate_on_call", false);
    }
}