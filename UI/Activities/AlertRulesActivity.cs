using Android;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Runtime.Versioning;
using Serilog;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.ViewModels;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/alert_rules", Exported = false)]
public sealed class AlertRulesActivity : LocalizedActivity
{
    private const int NotificationPermissionRequestCode = 2101;
    private readonly WatchedCallsignMatchTarget[] _watchedCallsignMatchTargets =
    [
        WatchedCallsignMatchTarget.TransmitterOnly,
        WatchedCallsignMatchTarget.ReceiverOnly,
        WatchedCallsignMatchTarget.ReceiverOrTransmitter
    ];
    private readonly SelectedDxccMatchTarget[] _selectedDxccMatchTargets =
    [
        SelectedDxccMatchTarget.TransmitterOnly,
        SelectedDxccMatchTarget.ReceiverOnly,
        SelectedDxccMatchTarget.ReceiverOrTransmitter
    ];
    private readonly SemaphoreSlim _pauseSaveLock = new(1, 1);
    private SettingsViewModel _viewModel = null!;
    private INotificationService _notificationService = null!;
    private bool _isBinding;
    private Button _manageCallsignPatternsButton = null!;
    private Button _openNotificationSettingsButton = null!;
    private Button _setDxccButton = null!;
    private Spinner _watchedCallsignMatchTargetSpinner = null!;
    private Spinner _selectedDxccMatchTargetSpinner = null!;
    private CheckBox _sendNotificationCheckbox = null!;
    private CheckBox _vibrationCheckbox = null!;
    private CheckBox _sendNotificationAllCheckbox = null!;
    private CheckBox _vibrationAllCheckbox = null!;
    private CheckBox _sendNotificationDxccCheckbox = null!;
    private CheckBox _vibrationDxccCheckbox = null!;
    private CheckBox _sendNotificationLoggedQsoCheckbox = null!;
    private CheckBox _vibrationLoggedQsoCheckbox = null!;
    private EditText _myCallCooldownValue = null!;
    private EditText _anyMessageCooldownValue = null!;
    private EditText _selectedDxccCooldownValue = null!;
    private EditText _loggedQsoCooldownValue = null!;
    private TextView _myCallCooldownHelp = null!;
    private TextView _anyMessageCooldownHelp = null!;
    private TextView _selectedDxccCooldownHelp = null!;
    private TextView _loggedQsoCooldownHelp = null!;
    private CheckBox? _pendingNotificationCheckbox;
    private Action<bool>? _pendingNotificationSetter;
    private bool _suppressNotificationToggleEvents;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_alert_rules);

        _viewModel = AppHost.Current.GetRequiredService<SettingsViewModel>();
        _notificationService = AppHost.Current.GetRequiredService<INotificationService>();
        _isBinding = true;
        BindViews();
        InitializeWatchedCallsignMatchTargetSpinner();
        InitializeSelectedDxccMatchTargetSpinner();
        BindEvents();
        _ = LoadAsync();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _ = LoadAsync();
    }

    protected override void OnPause()
    {
        if (!_isBinding)
        {
            _ = SaveOnPauseAsync();
        }

        base.OnPause();
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

        if (requestCode != NotificationPermissionRequestCode)
        {
            return;
        }

        var granted = grantResults.Length > 0 && grantResults[0] == Permission.Granted;
        if (granted)
        {
            _pendingNotificationSetter?.Invoke(true);
        }
        else if (_pendingNotificationCheckbox is not null)
        {
            _pendingNotificationSetter?.Invoke(false);
            SetNotificationCheckboxChecked(_pendingNotificationCheckbox, false);
            Toast.MakeText(this, GetString(Resource.String.denied_notification), ToastLength.Long)?.Show();
        }

        _pendingNotificationCheckbox = null;
        _pendingNotificationSetter = null;
        UpdateNotificationSettingsButtonVisibility();
    }

    private void BindViews()
    {
        _openNotificationSettingsButton = FindViewById<Button>(Resource.Id.open_notification_settings)!;
        _manageCallsignPatternsButton = FindViewById<Button>(Resource.Id.manage_callsign_patterns)!;
        _setDxccButton = FindViewById<Button>(Resource.Id.set_dxcc)!;
        _watchedCallsignMatchTargetSpinner = FindViewById<Spinner>(Resource.Id.watched_callsign_match_target_spinner)!;
        _selectedDxccMatchTargetSpinner = FindViewById<Spinner>(Resource.Id.selected_dxcc_match_target_spinner)!;
        _sendNotificationCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_checkbox)!;
        _vibrationCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_checkbox)!;
        _sendNotificationAllCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_all_checkbox)!;
        _vibrationAllCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_all_checkbox)!;
        _sendNotificationDxccCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_dxcc_checkbox)!;
        _vibrationDxccCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_dxcc_checkbox)!;
        _sendNotificationLoggedQsoCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_logged_qso_checkbox)!;
        _vibrationLoggedQsoCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_logged_qso_checkbox)!;
        _myCallCooldownValue = FindViewById<EditText>(Resource.Id.my_call_cooldown_value)!;
        _anyMessageCooldownValue = FindViewById<EditText>(Resource.Id.any_message_cooldown_value)!;
        _selectedDxccCooldownValue = FindViewById<EditText>(Resource.Id.selected_dxcc_cooldown_value)!;
        _loggedQsoCooldownValue = FindViewById<EditText>(Resource.Id.logged_qso_cooldown_value)!;
        _myCallCooldownHelp = FindViewById<TextView>(Resource.Id.my_call_cooldown_help)!;
        _anyMessageCooldownHelp = FindViewById<TextView>(Resource.Id.any_message_cooldown_help)!;
        _selectedDxccCooldownHelp = FindViewById<TextView>(Resource.Id.selected_dxcc_cooldown_help)!;
        _loggedQsoCooldownHelp = FindViewById<TextView>(Resource.Id.logged_qso_cooldown_help)!;
    }

    private void InitializeWatchedCallsignMatchTargetSpinner()
    {
        var labels = _watchedCallsignMatchTargets.Select(GetWatchedCallsignMatchTargetLabel).ToArray();
        var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, labels);
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        _watchedCallsignMatchTargetSpinner.Adapter = adapter;
    }

    private void InitializeSelectedDxccMatchTargetSpinner()
    {
        var labels = _selectedDxccMatchTargets.Select(GetSelectedDxccMatchTargetLabel).ToArray();
        var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, labels);
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        _selectedDxccMatchTargetSpinner.Adapter = adapter;
    }

    private void BindEvents()
    {
        _watchedCallsignMatchTargetSpinner.ItemSelected += (_, args) =>
        {
            if (_isBinding)
            {
                return;
            }

            _viewModel.WatchedCallsignMatchTarget = _watchedCallsignMatchTargets[Math.Clamp(args.Position, 0, _watchedCallsignMatchTargets.Length - 1)];
        };

        _selectedDxccMatchTargetSpinner.ItemSelected += (_, args) =>
        {
            if (_isBinding)
            {
                return;
            }

            _viewModel.SelectedDxccMatchTarget = _selectedDxccMatchTargets[Math.Clamp(args.Position, 0, _selectedDxccMatchTargets.Length - 1)];
        };

        _sendNotificationCheckbox.CheckedChange += (_, args) =>
            HandleNotificationToggle(_sendNotificationCheckbox, value => _viewModel.NotifyOnMyCall = value, args.IsChecked);
        _sendNotificationAllCheckbox.CheckedChange += (_, args) =>
            HandleNotificationToggle(_sendNotificationAllCheckbox, value => _viewModel.NotifyOnAnyMessage = value, args.IsChecked);
        _sendNotificationDxccCheckbox.CheckedChange += (_, args) =>
            HandleNotificationToggle(_sendNotificationDxccCheckbox, value => _viewModel.NotifyOnSelectedDxcc = value, args.IsChecked);
        _sendNotificationLoggedQsoCheckbox.CheckedChange += (_, args) =>
            HandleNotificationToggle(_sendNotificationLoggedQsoCheckbox, value => _viewModel.NotifyOnLoggedQso = value, args.IsChecked);

        _vibrationCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _viewModel.VibrateOnMyCall = args.IsChecked;
            }
        };
        _vibrationAllCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _viewModel.VibrateOnAnyMessage = args.IsChecked;
            }
        };
        _vibrationDxccCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _viewModel.VibrateOnSelectedDxcc = args.IsChecked;
            }
        };
        _vibrationLoggedQsoCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _viewModel.VibrateOnLoggedQso = args.IsChecked;
            }
        };

        _myCallCooldownValue.TextChanged += (_, _) =>
        {
            if (!_isBinding && TryReadNonNegativeInt(_myCallCooldownValue, out var value))
            {
                _viewModel.MyCallCooldownSeconds = value;
                UpdateCooldownHelp(_myCallCooldownHelp, value);
            }
        };
        _anyMessageCooldownValue.TextChanged += (_, _) =>
        {
            if (!_isBinding && TryReadNonNegativeInt(_anyMessageCooldownValue, out var value))
            {
                _viewModel.AnyMessageCooldownSeconds = value;
                UpdateCooldownHelp(_anyMessageCooldownHelp, value);
            }
        };
        _selectedDxccCooldownValue.TextChanged += (_, _) =>
        {
            if (!_isBinding && TryReadNonNegativeInt(_selectedDxccCooldownValue, out var value))
            {
                _viewModel.SelectedDxccCooldownSeconds = value;
                UpdateCooldownHelp(_selectedDxccCooldownHelp, value);
            }
        };
        _loggedQsoCooldownValue.TextChanged += (_, _) =>
        {
            if (!_isBinding && TryReadNonNegativeInt(_loggedQsoCooldownValue, out var value))
            {
                _viewModel.LoggedQsoCooldownSeconds = value;
                UpdateCooldownHelp(_loggedQsoCooldownHelp, value);
            }
        };

        _manageCallsignPatternsButton.Click += (_, _) => StartActivity(typeof(CallsignPatternActivity));
        _setDxccButton.Click += (_, _) => StartActivity(typeof(DxccSelectionActivity));
        _openNotificationSettingsButton.Click += (_, _) => _notificationService.OpenNotificationSettings();
    }

    private async Task LoadAsync()
    {
        _isBinding = true;
        await _viewModel.LoadAsync().ConfigureAwait(false);
        RunOnUiThread(() =>
        {
            _watchedCallsignMatchTargetSpinner.SetSelection(GetWatchedCallsignMatchTargetIndex(_viewModel.WatchedCallsignMatchTarget));
            _selectedDxccMatchTargetSpinner.SetSelection(GetSelectedDxccMatchTargetIndex(_viewModel.SelectedDxccMatchTarget));
            _manageCallsignPatternsButton.Text = $"{GetString(Resource.String.manage_callsign_patterns)} ({_viewModel.WatchedCallsignPatternCount})";
            _setDxccButton.Text = $"{GetString(Resource.String.set_dxcc_entity)} ({_viewModel.SelectedDxccCount})";
            _sendNotificationCheckbox.Checked = _viewModel.NotifyOnMyCall;
            _vibrationCheckbox.Checked = _viewModel.VibrateOnMyCall;
            _sendNotificationAllCheckbox.Checked = _viewModel.NotifyOnAnyMessage;
            _vibrationAllCheckbox.Checked = _viewModel.VibrateOnAnyMessage;
            _sendNotificationDxccCheckbox.Checked = _viewModel.NotifyOnSelectedDxcc;
            _vibrationDxccCheckbox.Checked = _viewModel.VibrateOnSelectedDxcc;
            _sendNotificationLoggedQsoCheckbox.Checked = _viewModel.NotifyOnLoggedQso;
            _vibrationLoggedQsoCheckbox.Checked = _viewModel.VibrateOnLoggedQso;
            _myCallCooldownValue.Text = _viewModel.MyCallCooldownSeconds.ToString();
            _anyMessageCooldownValue.Text = _viewModel.AnyMessageCooldownSeconds.ToString();
            _selectedDxccCooldownValue.Text = _viewModel.SelectedDxccCooldownSeconds.ToString();
            _loggedQsoCooldownValue.Text = _viewModel.LoggedQsoCooldownSeconds.ToString();
            UpdateCooldownHelp(_myCallCooldownHelp, _viewModel.MyCallCooldownSeconds);
            UpdateCooldownHelp(_anyMessageCooldownHelp, _viewModel.AnyMessageCooldownSeconds);
            UpdateCooldownHelp(_selectedDxccCooldownHelp, _viewModel.SelectedDxccCooldownSeconds);
            UpdateCooldownHelp(_loggedQsoCooldownHelp, _viewModel.LoggedQsoCooldownSeconds);
            UpdateNotificationSettingsButtonVisibility();
            _isBinding = false;
        });
    }

    private async Task SaveOnPauseAsync()
    {
        if (!await _pauseSaveLock.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            await _viewModel.SaveAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Failed to persist alert rules during activity pause.");
        }
        finally
        {
            _pauseSaveLock.Release();
        }
    }

    private void HandleNotificationToggle(CheckBox checkBox, Action<bool> setter, bool isChecked)
    {
        if (_isBinding || _suppressNotificationToggleEvents)
        {
            return;
        }

        if (!isChecked)
        {
            setter(false);
            UpdateNotificationSettingsButtonVisibility();
            return;
        }

        if (!RequiresNotificationPermissionRequest())
        {
            setter(true);
            UpdateNotificationSettingsButtonVisibility();
            return;
        }

        _pendingNotificationCheckbox = checkBox;
        _pendingNotificationSetter = setter;
        RequestNotificationPermission();
    }

    [SupportedOSPlatformGuard("android33.0")]
    private bool RequiresNotificationPermissionRequest()
    {
        return OperatingSystem.IsAndroidVersionAtLeast(33)
               && CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted;
    }

    [SupportedOSPlatform("android33.0")]
    private void RequestNotificationPermission()
    {
        RequestPermissions([Manifest.Permission.PostNotifications], NotificationPermissionRequestCode);
    }

    private void SetNotificationCheckboxChecked(CheckBox checkBox, bool isChecked)
    {
        _suppressNotificationToggleEvents = true;
        checkBox.Checked = isChecked;
        _suppressNotificationToggleEvents = false;
    }

    private void UpdateNotificationSettingsButtonVisibility()
    {
        _openNotificationSettingsButton.Visibility = _notificationService.AreNotificationsEnabled()
            ? ViewStates.Gone
            : ViewStates.Visible;
    }

    private void UpdateCooldownHelp(TextView textView, int seconds)
    {
        textView.Text = string.Format(GetString(Resource.String.alert_rule_cooldown_help), Math.Max(0, seconds));
    }

    private static bool TryReadNonNegativeInt(EditText editText, out int value)
    {
        return int.TryParse(editText.Text, out value) && value >= 0;
    }

    private int GetWatchedCallsignMatchTargetIndex(WatchedCallsignMatchTarget matchTarget)
    {
        var index = Array.IndexOf(_watchedCallsignMatchTargets, matchTarget);
        return index >= 0 ? index : 0;
    }

    private int GetSelectedDxccMatchTargetIndex(SelectedDxccMatchTarget matchTarget)
    {
        var index = Array.IndexOf(_selectedDxccMatchTargets, matchTarget);
        return index >= 0 ? index : 0;
    }

    private string GetWatchedCallsignMatchTargetLabel(WatchedCallsignMatchTarget matchTarget)
    {
        return matchTarget switch
        {
            WatchedCallsignMatchTarget.ReceiverOnly => GetString(Resource.String.watched_callsign_match_target_receiver),
            WatchedCallsignMatchTarget.ReceiverOrTransmitter => GetString(Resource.String.watched_callsign_match_target_both),
            _ => GetString(Resource.String.watched_callsign_match_target_transmitter)
        };
    }

    private string GetSelectedDxccMatchTargetLabel(SelectedDxccMatchTarget matchTarget)
    {
        return matchTarget switch
        {
            SelectedDxccMatchTarget.ReceiverOnly => GetString(Resource.String.selected_dxcc_match_target_receiver),
            SelectedDxccMatchTarget.ReceiverOrTransmitter => GetString(Resource.String.selected_dxcc_match_target_both),
            _ => GetString(Resource.String.selected_dxcc_match_target_transmitter)
        };
    }
}
