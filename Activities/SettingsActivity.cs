using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.OS;
using Android.Util;
using WsjtxWatcher.Behaviors.Watchers;
using WsjtxWatcher.Utils.Network;
using WsjtxWatcher.Variables;

namespace WsjtxWatcher.Activities;

[Activity(Label = "@string/settings", MainLauncher = false)]
public class SettingsActivity : Activity
{
    public static string Tag = "SettingsActivity";
    private Button _addBrandButton;
    private Button _addWhiteListButton;
    private OnCallsignChanged _callsignChanged;
    private TextView _callsignTextEdit;
    private Button _jumpToDxccButton;
    private OnLocationChanged _locationChanged;
    private TextView _locationTextEdit;
    private OnCheckedChanged _onChkChg;
    private OnResetAllListener Oras;
    private OnResetDbListener Ords;

    private OnPortChanged _portChanged;

    private TextView _portTextEdit;
    private Button _resetAllButton;
    private Button _resetDbButton;
    private CheckBox _sendNotificationAllCheckbox;
    private CheckBox _sendNotificationCheckbox;
    private CheckBox _sendNotificationDxccCheckbox;
    private CheckBox _vibrationAllCheckbox;
    private CheckBox _vibrationCheckbox;
    private CheckBox _vibrationDxccCheckbox;


    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Set our view from the "main" layout resource
        SetContentView(ResourceConstant.Layout.activity_settings);
        Log.Debug(Tag, "Settings Entered");
        // 检查联网情况
        var ipAddrTextView = FindViewById<TextView>(ResourceConstant.Id.ip_address_value);
        if (!Wifi.IsWificonnected(this))
            ipAddrTextView.Text = GetString(ResourceConstant.String.no_wifi);
        else
            ipAddrTextView.Text = Wifi.GetLocalIpAddress(this);

        // 设定端口
        _portTextEdit = FindViewById<TextView>(ResourceConstant.Id.port_value);
        _portTextEdit.Text = SettingsVariables.Port;
        _portChanged = new OnPortChanged(this);
        _portTextEdit.AddTextChangedListener(_portChanged);
        // 设定呼号
        _callsignTextEdit = FindViewById<TextView>(ResourceConstant.Id.callsign_value);
        _callsignTextEdit.Text = SettingsVariables.MyCallsign;
        _callsignChanged = new OnCallsignChanged(this);
        _callsignTextEdit.AddTextChangedListener(_callsignChanged);
        // 设定位置
        _locationTextEdit = FindViewById<TextView>(ResourceConstant.Id.location_value);
        _locationTextEdit.Text = SettingsVariables.MyLocation;
        _locationChanged = new OnLocationChanged(this);
        _locationTextEdit.AddTextChangedListener(_locationChanged);

        // 设定版本
        var versionTextView = FindViewById<TextView>(ResourceConstant.Id.version_value);
        versionTextView.Text = GlobalVariables.VersionTag;


        _onChkChg = new OnCheckedChanged(this);
        // 设定通知
        _sendNotificationCheckbox = FindViewById<CheckBox>(ResourceConstant.Id.send_notification_checkbox);
        _sendNotificationCheckbox.Checked = SettingsVariables.SendNotificationOnCall;
        _sendNotificationCheckbox.CheckedChange += _onChkChg.SendNotificationCheckboxChanged;

        // 设定振动
        _vibrationCheckbox = FindViewById<CheckBox>(ResourceConstant.Id.vibration_checkbox);
        _vibrationCheckbox.Checked = SettingsVariables.VibrateOnCall;
        _vibrationCheckbox.CheckedChange += _onChkChg.VibrateCheckboxChanged;

        // 设定全部通知
        _sendNotificationAllCheckbox = FindViewById<CheckBox>(ResourceConstant.Id.send_notification_all_checkbox);
        _sendNotificationAllCheckbox.Checked = SettingsVariables.SendNotificationOnAll;
        _sendNotificationAllCheckbox.CheckedChange += _onChkChg.SendNotificationAllCheckboxChanged;

        // 设定全部振动
        _vibrationAllCheckbox = FindViewById<CheckBox>(ResourceConstant.Id.vibration_all_checkbox);
        _vibrationAllCheckbox.Checked = SettingsVariables.VibrateOnAll;
        _vibrationAllCheckbox.CheckedChange += _onChkChg.VibrateAllCheckboxChanged;

        // 设定dxcc通知
        _sendNotificationDxccCheckbox = FindViewById<CheckBox>(ResourceConstant.Id.send_notification_dxcc_checkbox);
        _sendNotificationDxccCheckbox.Checked = SettingsVariables.SendNotificationOnDxcc;
        _sendNotificationDxccCheckbox.CheckedChange += _onChkChg.SendNotificationDxccCheckboxChanged;

        // 设定dxcc振动
        _vibrationDxccCheckbox = FindViewById<CheckBox>(ResourceConstant.Id.vibration_dxcc_checkbox);
        _vibrationDxccCheckbox.Checked = SettingsVariables.VibrateOnDxcc;
        _vibrationDxccCheckbox.CheckedChange += _onChkChg.VibrateDxccCheckboxChanged;


        Ords = new OnResetDbListener(this);
        _resetDbButton = FindViewById<Button>(ResourceConstant.Id.reset_database);
        _resetDbButton.SetOnClickListener(Ords);

        Oras = new OnResetAllListener(this);
        _resetAllButton = FindViewById<Button>(ResourceConstant.Id.reset_all);
        _resetAllButton.SetOnClickListener(Oras);

        _addWhiteListButton = FindViewById<Button>(ResourceConstant.Id.add_white_list);
        _addWhiteListButton.SetOnClickListener(new OnAddWhiteListListener());
        if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
        {
            var intent = new Intent();
            var packageName = PackageName;
            var pm = (PowerManager)GetSystemService(PowerService);
            if (pm.IsIgnoringBatteryOptimizations(packageName)) _addWhiteListButton.Enabled = false;
        }

        _addWhiteListButton = FindViewById<Button>(ResourceConstant.Id.add_background);
        _addWhiteListButton.SetOnClickListener(new OnAddBackgroundListener());

        _jumpToDxccButton = FindViewById<Button>(ResourceConstant.Id.set_dxcc);
        _jumpToDxccButton.SetOnClickListener(new OnJumpDxccListener(this));
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _portTextEdit.RemoveTextChangedListener(_portChanged);
        _callsignTextEdit.RemoveTextChangedListener(_callsignChanged);
        _locationTextEdit.RemoveTextChangedListener(_locationChanged);
        _sendNotificationCheckbox.CheckedChange -= _onChkChg.SendNotificationCheckboxChanged;
        _vibrationCheckbox.CheckedChange -= _onChkChg.VibrateCheckboxChanged;
    }
}