﻿using _Microsoft.Android.Resource.Designer;
using Android.Util;
using WsjtxWatcher.Behaviors.Watchers;
using WsjtxWatcher.Utils.Network;
using WsjtxWatcher.Variables;

namespace WsjtxWatcher.Activities;

[Activity(Label = "@string/settings", MainLauncher = false)]
public class SettingsActivity : Activity
{
    public static string TAG = "SettingsActivity";

    private TextView portTextEdit;
    private TextView callsignTextEdit;
    private TextView locationTextEdit;
    private CheckBox sendNotificationCheckbox;
    private CheckBox vibrationCheckbox;
    private Button resetDbButton;
    private Button resetAllButton;

    private OnPortChanged portChanged;
    private OnCallsignChanged callsignChanged;
    private OnLocationChanged locationChanged;
    private OnCheckedChanged onChkChg;
    protected OnResetDBListener ords;
    protected OnResetAllListener oras;
    

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Set our view from the "main" layout resource
        SetContentView(ResourceConstant.Layout.activity_settings);
        Log.Debug(TAG, "Settings Entered");
        // 检查联网情况
        var ipAddrTextView = FindViewById<TextView>(ResourceConstant.Id.ip_address_value);
        if (!Wifi.isWificonnected(this))
            ipAddrTextView.Text = GetString(ResourceConstant.String.no_wifi);
        else
            ipAddrTextView.Text = Wifi.getLocalIPAddress(this);

        // 设定端口
        portTextEdit = FindViewById<TextView>(ResourceConstant.Id.port_value);
        portTextEdit.Text = SettingsVariables.port;
        portChanged = new OnPortChanged(this);
        portTextEdit.AddTextChangedListener(portChanged);
        // 设定呼号
        callsignTextEdit = FindViewById<TextView>(ResourceConstant.Id.callsign_value);
        callsignTextEdit.Text = SettingsVariables.myCallsign;
        callsignChanged = new OnCallsignChanged(this);
        callsignTextEdit.AddTextChangedListener(callsignChanged);
        // 设定位置
        locationTextEdit = FindViewById<TextView>(ResourceConstant.Id.location_value);
        locationTextEdit.Text = SettingsVariables.myLocation;
        locationChanged = new OnLocationChanged(this);
        locationTextEdit.AddTextChangedListener(locationChanged);
        
        // 设定版本
        var versionTextView = FindViewById<TextView>(ResourceConstant.Id.version_value);
        versionTextView.Text = GlobalVariables.version_tag;


        onChkChg = new OnCheckedChanged(this);
        // 设定通知
        sendNotificationCheckbox = FindViewById<CheckBox>(ResourceConstant.Id.send_notification_checkbox);
        sendNotificationCheckbox.Checked = SettingsVariables.send_notification_on_call;
        sendNotificationCheckbox.CheckedChange += onChkChg.SendNotificationCheckboxChanged;
        
        // 设定振动
        vibrationCheckbox = FindViewById<CheckBox>(ResourceConstant.Id.vibration_checkbox);
        vibrationCheckbox.Checked = SettingsVariables.vibrate_on_call;
        vibrationCheckbox.CheckedChange += onChkChg.VibrateCheckboxChanged;

        
        ords = new OnResetDBListener();
        resetDbButton = FindViewById<Button>(ResourceConstant.Id.reset_database);
        resetDbButton.SetOnClickListener(ords);
        
        oras = new OnResetAllListener();
        resetAllButton = FindViewById<Button>(ResourceConstant.Id.reset_all);
        resetAllButton.SetOnClickListener(oras);
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        portTextEdit.RemoveTextChangedListener(portChanged);
        callsignTextEdit.RemoveTextChangedListener(callsignChanged);
        locationTextEdit.RemoveTextChangedListener(locationChanged);
        sendNotificationCheckbox.CheckedChange -= onChkChg.SendNotificationCheckboxChanged;
        vibrationCheckbox.CheckedChange -= onChkChg.VibrateCheckboxChanged;
    }
}