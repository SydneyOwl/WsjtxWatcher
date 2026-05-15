using Android;
using Android.App;
using Android.Content;
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

[Activity(Label = "@string/alert_rule_edit", Exported = false)]
public sealed class AlertRuleEditorActivity : LocalizedActivity
{
    public const string RuleIdExtra = "rule_id";
    private const int NotificationPermissionRequestCode = 2102;
    private readonly AlertRuleMatchTarget[] _matchTargets =
    [
        AlertRuleMatchTarget.TransmitterOnly,
        AlertRuleMatchTarget.ReceiverOnly,
        AlertRuleMatchTarget.ReceiverOrTransmitter
    ];

    private readonly SemaphoreSlim _pauseSaveLock = new(1, 1);
    private AlertRuleEditorViewModel _viewModel = null!;
    private INotificationService _notificationService = null!;
    private AlertRule _rule = null!;
    private bool _isBinding;
    private Spinner _matchTargetSpinner = null!;
    private LinearLayout _matchTargetSection = null!;
    private Button _manageCallsignPatternsButton = null!;
    private Button _openNotificationSettingsButton = null!;
    private Button _setDxccButton = null!;
    private CheckBox _enabledCheckbox = null!;
    private CheckBox _sendNotificationCheckbox = null!;
    private CheckBox _vibrationCheckbox = null!;
    private EditText _cooldownValue = null!;
    private TextView _cooldownHelp = null!;
    private TextView _titleView = null!;
    private CheckBox? _pendingNotificationCheckbox;
    private Action<bool>? _pendingNotificationSetter;
    private bool _suppressNotificationToggleEvents;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_alert_rule_editor);

        var ruleId = Intent?.GetStringExtra(RuleIdExtra);
        if (string.IsNullOrWhiteSpace(ruleId))
        {
            throw new InvalidOperationException("Alert rule id is required.");
        }

        _viewModel = AppHost.Current.GetRequiredService<AlertRuleEditorViewModel>();
        _notificationService = AppHost.Current.GetRequiredService<INotificationService>();
        BindViews();
        InitializeMatchTargetSpinner();
        BindEvents();
        _ = LoadAsync(ruleId);
    }

    protected override void OnResume()
    {
        base.OnResume();
        var ruleId = Intent?.GetStringExtra(RuleIdExtra);
        if (!string.IsNullOrWhiteSpace(ruleId))
        {
            _ = LoadAsync(ruleId);
        }
    }

    protected override void OnPause()
    {
        if (!_isBinding && _rule is not null)
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
        _titleView = FindViewById<TextView>(Resource.Id.alert_rule_title)!;
        _enabledCheckbox = FindViewById<CheckBox>(Resource.Id.alert_rule_enabled_checkbox)!;
        _sendNotificationCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_checkbox)!;
        _vibrationCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_checkbox)!;
        _openNotificationSettingsButton = FindViewById<Button>(Resource.Id.open_notification_settings)!;
        _cooldownValue = FindViewById<EditText>(Resource.Id.alert_rule_cooldown_value)!;
        _cooldownHelp = FindViewById<TextView>(Resource.Id.alert_rule_cooldown_help)!;
        _matchTargetSection = FindViewById<LinearLayout>(Resource.Id.alert_rule_match_target_section)!;
        _matchTargetSpinner = FindViewById<Spinner>(Resource.Id.alert_rule_match_target_spinner)!;
        _manageCallsignPatternsButton = FindViewById<Button>(Resource.Id.manage_callsign_patterns)!;
        _setDxccButton = FindViewById<Button>(Resource.Id.set_dxcc)!;
    }

    private void InitializeMatchTargetSpinner()
    {
        var labels = _matchTargets.Select(GetMatchTargetLabel).ToArray();
        var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, labels);
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        _matchTargetSpinner.Adapter = adapter;
    }

    private void BindEvents()
    {
        _enabledCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _rule.IsEnabled = args.IsChecked;
            }
        };

        _sendNotificationCheckbox.CheckedChange += (_, args) =>
            HandleNotificationToggle(_sendNotificationCheckbox, value => _rule.SendNotification = value, args.IsChecked);

        _vibrationCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _rule.Vibrate = args.IsChecked;
            }
        };

        _cooldownValue.TextChanged += (_, _) =>
        {
            if (!_isBinding && int.TryParse(_cooldownValue.Text, out var cooldownSeconds) && cooldownSeconds >= 0)
            {
                _rule.CooldownSeconds = cooldownSeconds;
                UpdateCooldownHelp();
            }
        };

        _matchTargetSpinner.ItemSelected += (_, args) =>
        {
            if (!_isBinding)
            {
                _rule.MatchTarget = _matchTargets[Math.Clamp(args.Position, 0, _matchTargets.Length - 1)];
            }
        };

        _manageCallsignPatternsButton.Click += (_, _) => StartActivity(typeof(CallsignPatternActivity));
        _setDxccButton.Click += (_, _) => StartActivity(typeof(DxccSelectionActivity));
        _openNotificationSettingsButton.Click += (_, _) => _notificationService.OpenNotificationSettings();
    }

    private async Task LoadAsync(string ruleId)
    {
        _isBinding = true;
        _rule = await _viewModel.LoadAsync(ruleId).ConfigureAwait(false);
        RunOnUiThread(() =>
        {
            Title = GetRuleTitle(_rule.Kind);
            _titleView.Text = GetRuleTitle(_rule.Kind);
            _enabledCheckbox.Checked = _rule.IsEnabled;
            _sendNotificationCheckbox.Checked = _rule.SendNotification;
            _vibrationCheckbox.Checked = _rule.Vibrate;
            _cooldownValue.Text = _rule.CooldownSeconds.ToString();
            _matchTargetSpinner.SetSelection(GetMatchTargetIndex(_rule.MatchTarget));
            _matchTargetSection.Visibility = _rule.Kind is AlertRuleKind.WatchedCallsign or AlertRuleKind.SelectedDxcc
                ? ViewStates.Visible
                : ViewStates.Gone;
            _manageCallsignPatternsButton.Visibility = _rule.Kind == AlertRuleKind.WatchedCallsign ? ViewStates.Visible : ViewStates.Gone;
            _setDxccButton.Visibility = _rule.Kind == AlertRuleKind.SelectedDxcc ? ViewStates.Visible : ViewStates.Gone;
            if (_rule.Kind == AlertRuleKind.WatchedCallsign)
            {
                var patternSummary = _rule.CallsignPatterns.Count == 0
                    ? GetString(Resource.String.alert_rule_default_callsign_patterns)
                    : _rule.CallsignPatterns.Count.ToString();
                _manageCallsignPatternsButton.Text = $"{GetString(Resource.String.manage_callsign_patterns)} ({patternSummary})";
            }

            if (_rule.Kind == AlertRuleKind.SelectedDxcc)
            {
                _setDxccButton.Text = $"{GetString(Resource.String.set_dxcc_entity)} ({_rule.SelectedDxccIds.Count})";
            }

            UpdateCooldownHelp();
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
            await _viewModel.SaveAsync(_rule).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Failed to save alert rule editor state.");
        }
        finally
        {
            _pauseSaveLock.Release();
        }
    }

    private void UpdateCooldownHelp()
    {
        _cooldownHelp.Text = string.Format(GetString(Resource.String.alert_rule_cooldown_help), Math.Max(0, _rule.CooldownSeconds));
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

    private string GetRuleTitle(AlertRuleKind kind)
    {
        return kind switch
        {
            AlertRuleKind.WatchedCallsign => GetString(Resource.String.when_callsign_included),
            AlertRuleKind.AnyMessage => GetString(Resource.String.when_call_all),
            AlertRuleKind.SelectedDxcc => GetString(Resource.String.on_dxcc),
            _ => GetString(Resource.String.on_logged_qso)
        };
    }

    private int GetMatchTargetIndex(AlertRuleMatchTarget matchTarget)
    {
        var index = Array.IndexOf(_matchTargets, matchTarget);
        return index >= 0 ? index : 0;
    }

    private string GetMatchTargetLabel(AlertRuleMatchTarget matchTarget)
    {
        return matchTarget switch
        {
            AlertRuleMatchTarget.ReceiverOnly => GetString(Resource.String.selected_dxcc_match_target_receiver),
            AlertRuleMatchTarget.ReceiverOrTransmitter => GetString(Resource.String.selected_dxcc_match_target_both),
            _ => GetString(Resource.String.selected_dxcc_match_target_transmitter)
        };
    }
}
