using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Collections.Specialized;
using System.ComponentModel;
using Serilog;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.ViewModels;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/settings", Exported = false)]
public sealed class SettingsActivity : LocalizedActivity
{
    private const string RepositoryUrl = "https://github.com/sydneyowl/wsjtxwatcher";
    private readonly DataSourceType[] _dataSourceTypes = [DataSourceType.Udp, DataSourceType.Relay];
    private readonly AppLanguage[] _supportedLanguages = [AppLanguage.SimplifiedChinese, AppLanguage.English];
    private readonly AppTheme[] _themeTypes = [AppTheme.FollowSystem, AppTheme.Light, AppTheme.Dark];
    private SettingsViewModel _viewModel = null!;
    private IAppThemeService _themeService = null!;
    private Spinner _themeSpinner = null!;
    private bool _isBinding;
    private Button _addBackgroundButton = null!;
    private Button _addWhitelistButton = null!;
    private EditText _callsignValue = null!;
    private Spinner _dataSourceSpinner = null!;
    private TextView _ipAddressValue = null!;
    private Button _configureAlertRulesButton = null!;
    private Button _manageDxccButton = null!;
    private Button _manageIgnoredCallsignsButton = null!;
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
    private TextView _relayPairingStatusValue = null!;
    private TextView _relaySelectedSourceValue = null!;
    private TextView _relayAdvancedActionsLabel = null!;
    private Button _relayTestConnectionButton = null!;
    private Button _relaySelectedSourceButton = null!;
    private Button _relayClearTrustButton = null!;
    private Button _resetAllButton = null!;
    private Button _resetDatabaseButton = null!;
    private Button _addTestDataButton = null!;
    private TextView _versionValue = null!;
    private CheckBox _autoIgnoreLoggedQsoCheckbox = null!;
    private INotificationService _notificationService = null!;
    private bool _isExitingApplication;
    private readonly SemaphoreSlim _pauseSaveLock = new(1, 1);

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_settings);

        _viewModel = AppHost.Current.GetRequiredService<SettingsViewModel>();
        _notificationService = AppHost.Current.GetRequiredService<INotificationService>();
        _themeService = AppHost.Current.GetRequiredService<IAppThemeService>();
        _isBinding = true;
        BindViews();
        InitializeDataSourceSpinner();
        InitializeLanguageSpinner();
        InitializeThemeSpinner();
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
        _relayPairingStatusValue = FindViewById<TextView>(Resource.Id.relay_pairing_status_value)!;
        _relaySelectedSourceValue = FindViewById<TextView>(Resource.Id.relay_selected_source_value)!;
        _relayAdvancedActionsLabel = FindViewById<TextView>(Resource.Id.relay_advanced_actions_label)!;
        _relayTestConnectionButton = FindViewById<Button>(Resource.Id.relay_test_connection_button)!;
        _relaySelectedSourceButton = FindViewById<Button>(Resource.Id.relay_selected_source_button)!;
        _relayClearTrustButton = FindViewById<Button>(Resource.Id.relay_clear_trust_button)!;
        _callsignValue = FindViewById<EditText>(Resource.Id.callsign_value)!;
        _locationValue = FindViewById<EditText>(Resource.Id.location_value)!;
        _versionValue = FindViewById<TextView>(Resource.Id.version_value)!;
        _languageSpinner = FindViewById<Spinner>(Resource.Id.language_spinner)!;
        _themeSpinner = FindViewById<Spinner>(Resource.Id.theme_spinner)!;
        _manageIgnoredCallsignsButton = FindViewById<Button>(Resource.Id.manage_ignored_callsigns)!;
        _manageDxccButton = FindViewById<Button>(Resource.Id.manage_dxcc)!;
        _openNotificationSettingsButton = FindViewById<Button>(Resource.Id.open_notification_settings)!;
        _configureAlertRulesButton = FindViewById<Button>(Resource.Id.configure_alert_rules)!;
        _resetDatabaseButton = FindViewById<Button>(Resource.Id.reset_database)!;
        _resetAllButton = FindViewById<Button>(Resource.Id.reset_all)!;
        _addTestDataButton = FindViewById<Button>(Resource.Id.add_test_data)!;
        _openLogButton = FindViewById<Button>(Resource.Id.open_log)!;
        _addWhitelistButton = FindViewById<Button>(Resource.Id.add_white_list)!;
        _addBackgroundButton = FindViewById<Button>(Resource.Id.add_background)!;
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

    private void InitializeThemeSpinner()
    {
        var labels = _themeTypes.Select(GetThemeLabel).ToArray();
        var adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleSpinnerItem, labels);
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        _themeSpinner.Adapter = adapter;
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

        _themeSpinner.ItemSelected += (_, args) =>
        {
            if (_isBinding)
            {
                return;
            }

            var selectedTheme = _themeTypes[Math.Clamp(args.Position, 0, _themeTypes.Length - 1)];
            _viewModel.SelectedTheme = selectedTheme;
            _themeService.ApplyTheme(selectedTheme);
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

#if DEBUG
        _addTestDataButton.Visibility = ViewStates.Visible;
        _addTestDataButton.Click += async (_, _) =>
        {
            await _viewModel.AddTestDataAsync().ConfigureAwait(false);
            RunOnUiThread(() => Toast.MakeText(this, Resource.String.test_data_added, ToastLength.Short)?.Show());
        };
#else
        _addTestDataButton.Visibility = ViewStates.Gone;
#endif

        _addWhitelistButton.Click += (_, _) => _viewModel.RequestIgnoreBatteryOptimizations();
        _addBackgroundButton.Click += (_, _) =>
        {
            _viewModel.OpenBackgroundSettings();
            ShowBackgroundHelpDialog();
        };

        _openNotificationSettingsButton.Click += (_, _) => _notificationService.OpenNotificationSettings();
        _configureAlertRulesButton.Click += (_, _) => StartActivity(typeof(AlertRulesActivity));
        _manageDxccButton.Click += (_, _) => StartActivity(typeof(DxccSelectionActivity));
        _relayTestConnectionButton.Click += async (_, _) => await TestRelayConnectionAsync().ConfigureAwait(false);
        _relaySelectedSourceButton.Click += async (_, _) =>
        {
            if (!CanSelectRelaySource())
            {
                RunOnUiThread(() => Toast.MakeText(this, Resource.String.relay_source_not_ready, ToastLength.Short)?.Show());
                return;
            }

            await _viewModel.RefreshRelaySourcesAsync().ConfigureAwait(false);
            RunOnUiThread(() => StartActivity(typeof(RelaySourceSelectionActivity)));
        };
        _relayClearTrustButton.Click += async (_, _) =>
        {
            _viewModel.RelayTrustedFingerprint = string.Empty;
            var saveResult = await _viewModel.SaveAsync().ConfigureAwait(false);
            await LoadAsync().ConfigureAwait(false);
            RunOnUiThread(() => ShowSaveResultToast(saveResult));
        };
        _manageIgnoredCallsignsButton.Click += (_, _) => StartActivity(typeof(IgnoredCallsignActivity));
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
            _themeSpinner.SetSelection(GetThemeIndex(_viewModel.SelectedTheme));
            _manageDxccButton.Text = $"{GetString(Resource.String.set_dxcc_entity)} ({_viewModel.SelectedDxccCount})";
            _manageIgnoredCallsignsButton.Text = $"{GetString(Resource.String.manage_ignored_callsigns)} ({_viewModel.IgnoredCallsignCount})";
            _autoIgnoreLoggedQsoCheckbox.Checked = _viewModel.AutoIgnoreLoggedQso;
            UpdateDataSourceSectionVisibility();
            UpdateRelaySection();
            UpdateNotificationSettingsButtonVisibility();
            _addWhitelistButton.Enabled = !_viewModel.IsIgnoringBatteryOptimizations;
            _configureAlertRulesButton.Text = GetString(Resource.String.configure_alert_rules);
            _isBinding = false;
        });
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

    private int GetThemeIndex(AppTheme theme)
    {
        var index = Array.IndexOf(_themeTypes, theme);
        return index >= 0 ? index : 0;
    }

    private string GetThemeLabel(AppTheme theme)
    {
        return theme switch
        {
            AppTheme.Light => GetString(Resource.String.theme_light),
            AppTheme.Dark => GetString(Resource.String.theme_dark),
            _ => GetString(Resource.String.theme_follow_system)
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

    private void UpdateDataSourceSectionVisibility()
    {
        var isRelay = _viewModel.SelectedDataSourceType == DataSourceType.Relay;
        _udpSection.Visibility = isRelay ? ViewStates.Gone : ViewStates.Visible;
        _relaySection.Visibility = isRelay ? ViewStates.Visible : ViewStates.Gone;
    }

    private void UpdateNotificationSettingsButtonVisibility()
    {
        _openNotificationSettingsButton.Visibility = _notificationService.AreNotificationsEnabled()
            ? ViewStates.Gone
            : ViewStates.Visible;
    }

    private void UpdateRelaySection()
    {
        var runtimeState = _viewModel.RelayRuntimeState;
        _relayConnectionStatusValue.Text = string.IsNullOrWhiteSpace(runtimeState.ConnectionStatus)
            ? GetString(Resource.String.wait_conn)
            : runtimeState.ConnectionStatus;

        var isPaired = !string.IsNullOrWhiteSpace(_viewModel.RelayTrustedFingerprint);
        _relayPairingStatusValue.Text = GetString(isPaired
            ? Resource.String.relay_pairing_status_paired
            : Resource.String.relay_pairing_status_not_paired);

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

        _relaySelectedSourceValue.Text = selectedSourceName;
        _relaySelectedSourceButton.Enabled = CanSelectRelaySource();
        _relayClearTrustButton.Visibility = isPaired ? ViewStates.Visible : ViewStates.Gone;
        _relayAdvancedActionsLabel.Visibility = isPaired ? ViewStates.Visible : ViewStates.Gone;
    }

    private bool CanSelectRelaySource()
    {
        return !string.IsNullOrWhiteSpace(_viewModel.RelayTrustedFingerprint);
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
            if (result.Success && string.IsNullOrWhiteSpace(_viewModel.RelayTrustedFingerprint) && !string.IsNullOrWhiteSpace(result.ObservedFingerprint))
            {
                _viewModel.RelayTrustedFingerprint = result.ObservedFingerprint;
            }

            RunOnUiThread(() =>
            {
                _relayConnectionStatusValue.Text = result.Message;
                UpdateRelaySection();
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
