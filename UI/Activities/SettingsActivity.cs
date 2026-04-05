using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.Versioning;
using Serilog;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.ViewModels;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/settings", Exported = false)]
public sealed class SettingsActivity : LocalizedActivity
{
    private const int NotificationPermissionRequestCode = 2001;
    private const string RepositoryUrl = "https://github.com/sydneyowl/wsjtxwatcher";
    private readonly IgnoredCallsignMatchTarget[] _ignoredCallsignMatchTargets =
    [
        IgnoredCallsignMatchTarget.Disabled,
        IgnoredCallsignMatchTarget.TransmitterOnly,
        IgnoredCallsignMatchTarget.ReceiverOnly,
        IgnoredCallsignMatchTarget.ReceiverOrTransmitter
    ];
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
    private readonly DataSourceType[] _dataSourceTypes = [DataSourceType.Udp, DataSourceType.Relay];
    private readonly AppLanguage[] _supportedLanguages = [AppLanguage.SimplifiedChinese, AppLanguage.English];
    private SettingsViewModel _viewModel = null!;
    private bool _isBinding;
    private Button _addBackgroundButton = null!;
    private Button _addWhitelistButton = null!;
    private EditText _callsignValue = null!;
    private Spinner _dataSourceSpinner = null!;
    private TextView _ipAddressValue = null!;
    private Spinner _ignoredCallsignMatchTargetSpinner = null!;
    private Spinner _watchedCallsignMatchTargetSpinner = null!;
    private Spinner _selectedDxccMatchTargetSpinner = null!;
    private Button _manageIgnoredCallsignsButton = null!;
    private Button _manageCallsignPatternsButton = null!;
    private Button _openNotificationSettingsButton = null!;
    private Button _openLogButton = null!;
    private EditText _locationValue = null!;
    private Spinner _languageSpinner = null!;
    private EditText _portValue = null!;
    private LinearLayout _udpSection = null!;
    private LinearLayout _relaySection = null!;
    private EditText _relayServerUrlValue = null!;
    private EditText _relaySharedSecretValue = null!;
    private EditText _relayTenantIdValue = null!;
    private TextView _relayConnectionStatusValue = null!;
    private Button _relayTestConnectionButton = null!;
    private Button _relaySelectedSourceButton = null!;
    private Button _relayRefreshSourcesButton = null!;
    private Button _relayClearTrustButton = null!;
    private Button _resetAllButton = null!;
    private Button _resetDatabaseButton = null!;
    private CheckBox _sendNotificationAllCheckbox = null!;
    private CheckBox _sendNotificationCheckbox = null!;
    private CheckBox _sendNotificationDxccCheckbox = null!;
    private CheckBox _sendNotificationLoggedQsoCheckbox = null!;
    private Button _setDxccButton = null!;
    private TextView _versionValue = null!;
    private CheckBox _autoIgnoreLoggedQsoCheckbox = null!;
    private CheckBox _vibrationAllCheckbox = null!;
    private CheckBox _vibrationCheckbox = null!;
    private CheckBox _vibrationDxccCheckbox = null!;
    private CheckBox _vibrationLoggedQsoCheckbox = null!;
    private INotificationService _notificationService = null!;
    private CheckBox? _pendingNotificationCheckbox;
    private Action<bool>? _pendingNotificationSetter;
    private bool _suppressNotificationToggleEvents;
    private bool _isExitingApplication;
    private readonly SemaphoreSlim _pauseSaveLock = new(1, 1);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_settings);

        _viewModel = AppHost.Current.GetRequiredService<SettingsViewModel>();
        _notificationService = AppHost.Current.GetRequiredService<INotificationService>();
        _isBinding = true;
        BindViews();
        InitializeDataSourceSpinner();
        InitializeLanguageSpinner();
        InitializeIgnoredCallsignMatchTargetSpinner();
        InitializeWatchedCallsignMatchTargetSpinner();
        InitializeSelectedDxccMatchTargetSpinner();
        BindEvents();
        _viewModel.RelayRuntimeState.PropertyChanged += OnRelayRuntimeStateChanged;
        _viewModel.RelayRuntimeState.Sources.CollectionChanged += OnRelaySourcesChanged;
        _ = LoadAsync();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _ = LoadAsync();
    }

    protected override void OnPause()
    {
        if (!_isBinding && !_isExitingApplication)
        {
            _ = SaveOnPauseAsync();
        }

        base.OnPause();
    }

    protected override void OnDestroy()
    {
        if (_viewModel is not null)
        {
            _viewModel.RelayRuntimeState.PropertyChanged -= OnRelayRuntimeStateChanged;
            _viewModel.RelayRuntimeState.Sources.CollectionChanged -= OnRelaySourcesChanged;
        }

        base.OnDestroy();
    }

    private void BindViews()
    {
        _dataSourceSpinner = FindViewById<Spinner>(Resource.Id.data_source_spinner)!;
        _ipAddressValue = FindViewById<TextView>(Resource.Id.ip_address_value)!;
        _portValue = FindViewById<EditText>(Resource.Id.port_value)!;
        _udpSection = FindViewById<LinearLayout>(Resource.Id.udp_section)!;
        _relaySection = FindViewById<LinearLayout>(Resource.Id.relay_section)!;
        _relayServerUrlValue = FindViewById<EditText>(Resource.Id.relay_server_url_value)!;
        _relaySharedSecretValue = FindViewById<EditText>(Resource.Id.relay_shared_secret_value)!;
        _relayTenantIdValue = FindViewById<EditText>(Resource.Id.relay_tenant_id_value)!;
        _relayConnectionStatusValue = FindViewById<TextView>(Resource.Id.relay_connection_status_value)!;
        _relayTestConnectionButton = FindViewById<Button>(Resource.Id.relay_test_connection_button)!;
        _relaySelectedSourceButton = FindViewById<Button>(Resource.Id.relay_selected_source_button)!;
        _relayRefreshSourcesButton = FindViewById<Button>(Resource.Id.relay_refresh_sources_button)!;
        _relayClearTrustButton = FindViewById<Button>(Resource.Id.relay_clear_trust_button)!;
        _callsignValue = FindViewById<EditText>(Resource.Id.callsign_value)!;
        _locationValue = FindViewById<EditText>(Resource.Id.location_value)!;
        _versionValue = FindViewById<TextView>(Resource.Id.version_value)!;
        _languageSpinner = FindViewById<Spinner>(Resource.Id.language_spinner)!;
        _ignoredCallsignMatchTargetSpinner = FindViewById<Spinner>(Resource.Id.ignored_callsign_match_target_spinner)!;
        _watchedCallsignMatchTargetSpinner = FindViewById<Spinner>(Resource.Id.watched_callsign_match_target_spinner)!;
        _selectedDxccMatchTargetSpinner = FindViewById<Spinner>(Resource.Id.selected_dxcc_match_target_spinner)!;
        _manageCallsignPatternsButton = FindViewById<Button>(Resource.Id.manage_callsign_patterns)!;
        _manageIgnoredCallsignsButton = FindViewById<Button>(Resource.Id.manage_ignored_callsigns)!;
        _openNotificationSettingsButton = FindViewById<Button>(Resource.Id.open_notification_settings)!;
        _sendNotificationCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_checkbox)!;
        _vibrationCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_checkbox)!;
        _sendNotificationAllCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_all_checkbox)!;
        _vibrationAllCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_all_checkbox)!;
        _sendNotificationDxccCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_dxcc_checkbox)!;
        _sendNotificationLoggedQsoCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_logged_qso_checkbox)!;
        _vibrationDxccCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_dxcc_checkbox)!;
        _vibrationLoggedQsoCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_logged_qso_checkbox)!;
        _resetDatabaseButton = FindViewById<Button>(Resource.Id.reset_database)!;
        _resetAllButton = FindViewById<Button>(Resource.Id.reset_all)!;
        _openLogButton = FindViewById<Button>(Resource.Id.open_log)!;
        _addWhitelistButton = FindViewById<Button>(Resource.Id.add_white_list)!;
        _addBackgroundButton = FindViewById<Button>(Resource.Id.add_background)!;
        _setDxccButton = FindViewById<Button>(Resource.Id.set_dxcc)!;
        _autoIgnoreLoggedQsoCheckbox = FindViewById<CheckBox>(Resource.Id.auto_ignore_logged_qso_checkbox)!;
    }

    private void InitializeDataSourceSpinner()
    {
        var labels = _dataSourceTypes.Select(GetDataSourceLabel).ToArray();
        var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, labels);
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        _dataSourceSpinner.Adapter = adapter;
    }

    private void InitializeLanguageSpinner()
    {
        var labels = _supportedLanguages.Select(GetLanguageLabel).ToArray();
        var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, labels);
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        _languageSpinner.Adapter = adapter;
    }

    private void InitializeIgnoredCallsignMatchTargetSpinner()
    {
        var labels = _ignoredCallsignMatchTargets.Select(GetIgnoredCallsignMatchTargetLabel).ToArray();
        var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, labels);
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        _ignoredCallsignMatchTargetSpinner.Adapter = adapter;
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
        _dataSourceSpinner.ItemSelected += (_, args) =>
        {
            if (_isBinding)
            {
                return;
            }

            _viewModel.SelectedDataSourceType = _dataSourceTypes[Math.Clamp(args.Position, 0, _dataSourceTypes.Length - 1)];
            RunOnUiThread(UpdateDataSourceSectionVisibility);
        };

        _languageSpinner.ItemSelected += async (_, args) =>
        {
            if (_isBinding)
            {
                return;
            }

            var selectedLanguage = _supportedLanguages[Math.Clamp(args.Position, 0, _supportedLanguages.Length - 1)];
            if (_viewModel.SelectedLanguage == selectedLanguage)
            {
                return;
            }

            _viewModel.SelectedLanguage = selectedLanguage;
            if (_viewModel.IsLanguageChangePending)
            {
                var shouldExit = await ConfirmAsync(
                    Resource.String.language_restart_title,
                    Resource.String.language_restart_message).ConfigureAwait(false);
                if (!shouldExit)
                {
                    RunOnUiThread(RevertLanguageSelection);
                    return;
                }

                _isExitingApplication = true;
                await _viewModel.SaveAndStopAsync().ConfigureAwait(false);
                RunOnUiThread(() =>
                {
                    StopService(new Intent(this, typeof(UI.Services.MsgPushService)));
                    ExitApplication();
                });
            }
        };

        _ignoredCallsignMatchTargetSpinner.ItemSelected += (_, args) =>
        {
            if (_isBinding)
            {
                return;
            }

            _viewModel.IgnoredCallsignMatchTarget = _ignoredCallsignMatchTargets[Math.Clamp(args.Position, 0, _ignoredCallsignMatchTargets.Length - 1)];
        };

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

        _portValue.TextChanged += (_, _) =>
        {
            if (!_isBinding)
            {
                _viewModel.Port = _portValue.Text ?? string.Empty;
            }
        };

        _relayServerUrlValue.TextChanged += (_, _) =>
        {
            if (!_isBinding)
            {
                _viewModel.RelayServerUrl = _relayServerUrlValue.Text ?? string.Empty;
            }
        };

        _relaySharedSecretValue.TextChanged += (_, _) =>
        {
            if (!_isBinding)
            {
                _viewModel.RelaySharedSecret = _relaySharedSecretValue.Text ?? string.Empty;
            }
        };

        _relayTenantIdValue.TextChanged += (_, _) =>
        {
            if (!_isBinding)
            {
                _viewModel.RelayTenantId = _relayTenantIdValue.Text ?? string.Empty;
            }
        };

        _callsignValue.TextChanged += (_, _) =>
        {
            if (!_isBinding)
            {
                _viewModel.MyCallsign = _callsignValue.Text ?? string.Empty;
            }
        };

        _locationValue.TextChanged += (_, _) =>
        {
            if (!_isBinding)
            {
                _viewModel.MyGrid = _locationValue.Text ?? string.Empty;
            }
        };

        _sendNotificationCheckbox.CheckedChange += (_, args) =>
            HandleNotificationToggle(_sendNotificationCheckbox, value => _viewModel.NotifyOnMyCall = value, args.IsChecked);

        _vibrationCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _viewModel.VibrateOnMyCall = args.IsChecked;
            }
        };

        _sendNotificationAllCheckbox.CheckedChange += (_, args) =>
            HandleNotificationToggle(_sendNotificationAllCheckbox, value => _viewModel.NotifyOnAnyMessage = value, args.IsChecked);

        _vibrationAllCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _viewModel.VibrateOnAnyMessage = args.IsChecked;
            }
        };

        _sendNotificationDxccCheckbox.CheckedChange += (_, args) =>
            HandleNotificationToggle(_sendNotificationDxccCheckbox, value => _viewModel.NotifyOnSelectedDxcc = value, args.IsChecked);

        _sendNotificationLoggedQsoCheckbox.CheckedChange += (_, args) =>
            HandleNotificationToggle(_sendNotificationLoggedQsoCheckbox, value => _viewModel.NotifyOnLoggedQso = value, args.IsChecked);

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

        _autoIgnoreLoggedQsoCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _viewModel.AutoIgnoreLoggedQso = args.IsChecked;
            }
        };

        _resetDatabaseButton.Click += async (_, _) =>
        {
            if (!await ConfirmAsync(Resource.String.reset_database, Resource.String.reset_database_confirm).ConfigureAwait(false))
            {
                return;
            }

            await _viewModel.ResetCacheAsync().ConfigureAwait(false);
            RunOnUiThread(() => Toast.MakeText(this, Resource.String.reset_database, ToastLength.Short)?.Show());
        };

        _resetAllButton.Click += async (_, _) =>
        {
            if (!await ConfirmAsync(Resource.String.reset_all, Resource.String.reset_all_confirm).ConfigureAwait(false))
            {
                return;
            }

            await _viewModel.ResetAllAsync().ConfigureAwait(false);
            await LoadAsync().ConfigureAwait(false);
        };

        _openLogButton.Click += (_, _) =>
        {
            try
            {
                _viewModel.OpenLogFile();
            }
            catch
            {
                Toast.MakeText(this, GetString(Resource.String.no_app_found), ToastLength.Short)?.Show();
            }
        };

        _addWhitelistButton.Click += (_, _) => _viewModel.RequestIgnoreBatteryOptimizations();
        _addBackgroundButton.Click += (_, _) =>
        {
            _viewModel.OpenBackgroundSettings();
            ShowBackgroundHelpDialog();
        };

        _openNotificationSettingsButton.Click += (_, _) => _notificationService.OpenNotificationSettings();
        _relayTestConnectionButton.Click += async (_, _) => await TestRelayConnectionAsync().ConfigureAwait(false);
        _relaySelectedSourceButton.Click += (_, _) => StartActivity(typeof(RelaySourceSelectionActivity));
        _relayRefreshSourcesButton.Click += async (_, _) => await _viewModel.RefreshRelaySourcesAsync().ConfigureAwait(false);
        _relayClearTrustButton.Click += async (_, _) =>
        {
            _viewModel.RelayTrustedFingerprint = string.Empty;
            var saveResult = await _viewModel.SaveAsync().ConfigureAwait(false);
            await LoadAsync().ConfigureAwait(false);
            RunOnUiThread(() => ShowSaveResultToast(saveResult));
        };
        _manageIgnoredCallsignsButton.Click += (_, _) => StartActivity(typeof(IgnoredCallsignActivity));
        _manageCallsignPatternsButton.Click += (_, _) => StartActivity(typeof(CallsignPatternActivity));
        _setDxccButton.Click += (_, _) => StartActivity(typeof(DxccSelectionActivity));
        _versionValue.Click += (_, _) =>
        {
            try
            {
                StartActivity(new Intent(Intent.ActionView, Android.Net.Uri.Parse(RepositoryUrl)));
            }
            catch
            {
                Toast.MakeText(this, GetString(Resource.String.no_app_found), ToastLength.Short)?.Show();
            }
        };
    }

    private async Task LoadAsync()
    {
        _isBinding = true;
        await _viewModel.LoadAsync().ConfigureAwait(false);
        RunOnUiThread(() =>
        {
            _dataSourceSpinner.SetSelection(GetDataSourceIndex(_viewModel.SelectedDataSourceType));
            _ipAddressValue.Text = string.IsNullOrWhiteSpace(_viewModel.LocalIpAddress)
                ? GetString(Resource.String.no_wifi)
                : _viewModel.LocalIpAddress;
            _portValue.Text = _viewModel.Port;
            _relayServerUrlValue.Text = _viewModel.RelayServerUrl;
            _relaySharedSecretValue.Text = _viewModel.RelaySharedSecret;
            _relayTenantIdValue.Text = _viewModel.RelayTenantId;
            _callsignValue.Text = _viewModel.MyCallsign;
            _locationValue.Text = _viewModel.MyGrid;
            _versionValue.Text = $"{_viewModel.VersionName}";
            _versionValue.PaintFlags |= PaintFlags.UnderlineText;
            _languageSpinner.SetSelection(GetLanguageIndex(_viewModel.SelectedLanguage));
            _ignoredCallsignMatchTargetSpinner.SetSelection(GetIgnoredCallsignMatchTargetIndex(_viewModel.IgnoredCallsignMatchTarget));
            _watchedCallsignMatchTargetSpinner.SetSelection(GetWatchedCallsignMatchTargetIndex(_viewModel.WatchedCallsignMatchTarget));
            _selectedDxccMatchTargetSpinner.SetSelection(GetSelectedDxccMatchTargetIndex(_viewModel.SelectedDxccMatchTarget));
            _manageCallsignPatternsButton.Text = $"{GetString(Resource.String.manage_callsign_patterns)} ({_viewModel.WatchedCallsignPatternCount})";
            _manageIgnoredCallsignsButton.Text = $"{GetString(Resource.String.manage_ignored_callsigns)} ({_viewModel.IgnoredCallsignCount})";
            _sendNotificationCheckbox.Checked = _viewModel.NotifyOnMyCall;
            _vibrationCheckbox.Checked = _viewModel.VibrateOnMyCall;
            _sendNotificationAllCheckbox.Checked = _viewModel.NotifyOnAnyMessage;
            _vibrationAllCheckbox.Checked = _viewModel.VibrateOnAnyMessage;
            _sendNotificationDxccCheckbox.Checked = _viewModel.NotifyOnSelectedDxcc;
            _vibrationDxccCheckbox.Checked = _viewModel.VibrateOnSelectedDxcc;
            _sendNotificationLoggedQsoCheckbox.Checked = _viewModel.NotifyOnLoggedQso;
            _vibrationLoggedQsoCheckbox.Checked = _viewModel.VibrateOnLoggedQso;
            _autoIgnoreLoggedQsoCheckbox.Checked = _viewModel.AutoIgnoreLoggedQso;
            UpdateNotificationSettingsButtonVisibility();
            UpdateDataSourceSectionVisibility();
            UpdateRelaySection();
            _addWhitelistButton.Enabled = !_viewModel.IsIgnoringBatteryOptimizations;
            _setDxccButton.Text = $"{GetString(Resource.String.set_dxcc_entity)} ({_viewModel.SelectedDxccCount})";
            _isBinding = false;
        });
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

    private int GetDataSourceIndex(DataSourceType dataSourceType)
    {
        var index = Array.IndexOf(_dataSourceTypes, dataSourceType);
        return index >= 0 ? index : 0;
    }

    private string GetDataSourceLabel(DataSourceType dataSourceType)
    {
        return dataSourceType == DataSourceType.Relay
            ? GetString(Resource.String.data_source_relay)
            : GetString(Resource.String.data_source_udp);
    }

    private int GetLanguageIndex(AppLanguage language)
    {
        var index = Array.IndexOf(_supportedLanguages, language);
        return index >= 0 ? index : 0;
    }

    private string GetLanguageLabel(AppLanguage language)
    {
        return language switch
        {
            AppLanguage.SimplifiedChinese => GetString(Resource.String.simplified_chinese),
            _ => GetString(Resource.String.english)
        };
    }

    private int GetIgnoredCallsignMatchTargetIndex(IgnoredCallsignMatchTarget matchTarget)
    {
        var index = Array.IndexOf(_ignoredCallsignMatchTargets, matchTarget);
        return index >= 0 ? index : 0;
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

    private string GetIgnoredCallsignMatchTargetLabel(IgnoredCallsignMatchTarget matchTarget)
    {
        return matchTarget switch
        {
            IgnoredCallsignMatchTarget.Disabled => GetString(Resource.String.ignored_callsign_match_target_disabled),
            IgnoredCallsignMatchTarget.ReceiverOnly => GetString(Resource.String.ignored_callsign_match_target_receiver),
            IgnoredCallsignMatchTarget.ReceiverOrTransmitter => GetString(Resource.String.ignored_callsign_match_target_both),
            _ => GetString(Resource.String.ignored_callsign_match_target_transmitter)
        };
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

    private Task<bool> ConfirmAsync(int titleResId, int messageResId)
    {
        var tcs = new TaskCompletionSource<bool>();
        RunOnUiThread(() =>
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle(titleResId);
            builder.SetMessage(messageResId);
            builder.SetPositiveButton(Android.Resource.String.Ok, (_, _) => tcs.TrySetResult(true));
            builder.SetNegativeButton(Android.Resource.String.Cancel, (_, _) => tcs.TrySetResult(false));
            var dialog = builder.Create() ?? throw new InvalidOperationException("Failed to create confirmation dialog.");
            dialog.CancelEvent += (_, _) => tcs.TrySetResult(false);
            dialog.Show();
        });
        return tcs.Task;
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
        return OperatingSystem.IsAndroidVersionAtLeast(33) &&
               CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted;
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

    private void UpdateDataSourceSectionVisibility()
    {
        var isRelay = _viewModel.SelectedDataSourceType == DataSourceType.Relay;
        _udpSection.Visibility = isRelay ? ViewStates.Gone : ViewStates.Visible;
        _relaySection.Visibility = isRelay ? ViewStates.Visible : ViewStates.Gone;
    }

    private void UpdateRelaySection()
    {
        var runtimeState = _viewModel.RelayRuntimeState;
        _relayConnectionStatusValue.Text = string.IsNullOrWhiteSpace(runtimeState.ConnectionStatus)
            ? GetString(Resource.String.wait_conn)
            : runtimeState.ConnectionStatus;

        var selectedSourceName = string.IsNullOrWhiteSpace(_viewModel.RelayPreferredSourceName)
            ? GetString(Resource.String.relay_source_unknown)
            : _viewModel.RelayPreferredSourceName;
        var selectedSource = runtimeState.Sources.FirstOrDefault(source =>
            string.Equals(source.SourceName, _viewModel.RelayPreferredSourceName, StringComparison.OrdinalIgnoreCase));
        if (selectedSource is not null)
        {
            selectedSourceName = string.Format(
                GetString(Resource.String.relay_source_status_format),
                selectedSource.DisplayName,
                GetString(selectedSource.Online ? Resource.String.relay_source_status_online : Resource.String.relay_source_status_offline));
        }

        _relaySelectedSourceButton.Text = selectedSourceName;
        _relayClearTrustButton.Enabled = !string.IsNullOrWhiteSpace(_viewModel.RelayTrustedFingerprint);
    }

    private void OnRelayRuntimeStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        RunOnUiThread(UpdateRelaySection);
    }

    private void OnRelaySourcesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RunOnUiThread(UpdateRelaySection);
    }

    private void ShowBackgroundHelpDialog()
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.background_help_title);
        builder.SetMessage(Resource.String.background_help_message);
        builder.SetPositiveButton(Android.Resource.String.Ok, (_, _) => { });
        builder.Show();
    }

    private void RevertLanguageSelection()
    {
        _isBinding = true;
        var originalLanguage = _viewModel.CurrentAppLanguage;
        _viewModel.SelectedLanguage = originalLanguage;
        _languageSpinner.SetSelection(GetLanguageIndex(originalLanguage));
        _isBinding = false;
    }

    private async Task SaveOnPauseAsync()
    {
        if (!await _pauseSaveLock.WaitAsync(0).ConfigureAwait(false))
        {
            return;
        }

        try
        {
            var saveResult = await _viewModel.SaveAsync().ConfigureAwait(false);
            RunOnUiThread(() => ShowSaveResultToast(saveResult));
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Failed to persist settings during activity pause.");
        }
        finally
        {
            _pauseSaveLock.Release();
        }
    }

    private void ExitApplication()
    {
        FinishAffinity();
        FinishAndRemoveTask();
        Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
        Java.Lang.JavaSystem.Exit(0);
    }

    private async Task TestRelayConnectionAsync()
    {
        RunOnUiThread(() =>
        {
            _relayTestConnectionButton.Enabled = false;
            _relayConnectionStatusValue.Text = GetString(Resource.String.relay_test_in_progress);
        });

        try
        {
            var result = await _viewModel.TestRelayConnectionAsync().ConfigureAwait(false);
            RunOnUiThread(() =>
            {
                _relayConnectionStatusValue.Text = result.Message;
                var toastMessage = result.Success && !_viewModel.IsWatcherServiceRunning
                    ? GetString(Resource.String.relay_test_success_manual_start)
                    : result.Message;
                Toast.MakeText(this, toastMessage, ToastLength.Long)?.Show();
            });
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Relay connection test failed unexpectedly.");
            RunOnUiThread(() =>
            {
                _relayConnectionStatusValue.Text = exception.Message;
                Toast.MakeText(this, exception.Message, ToastLength.Long)?.Show();
            });
        }
        finally
        {
            RunOnUiThread(() => _relayTestConnectionButton.Enabled = true);
        }
    }

    private void ShowSaveResultToast(SettingsSaveResult saveResult)
    {
        if (saveResult.ServiceRestarted)
        {
            Toast.MakeText(this, Resource.String.relay_save_service_restarted, ToastLength.Long)?.Show();
            return;
        }

        if (saveResult.ManualStartRecommended)
        {
            Toast.MakeText(this, Resource.String.relay_save_manual_start_required, ToastLength.Long)?.Show();
        }
    }
}
