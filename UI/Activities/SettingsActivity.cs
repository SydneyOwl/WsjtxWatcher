using Android.App;
using Android.OS;
using Android.Widget;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.ViewModels;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/settings", Exported = false)]
public sealed class SettingsActivity : Activity
{
    private SettingsViewModel _viewModel = null!;
    private bool _isBinding;
    private Button _addBackgroundButton = null!;
    private Button _addWhitelistButton = null!;
    private EditText _callsignValue = null!;
    private TextView _ipAddressValue = null!;
    private Button _openLogButton = null!;
    private EditText _locationValue = null!;
    private EditText _portValue = null!;
    private Button _resetAllButton = null!;
    private Button _resetDatabaseButton = null!;
    private CheckBox _sendNotificationAllCheckbox = null!;
    private CheckBox _sendNotificationCheckbox = null!;
    private CheckBox _sendNotificationDxccCheckbox = null!;
    private Button _setDxccButton = null!;
    private TextView _versionValue = null!;
    private CheckBox _vibrationAllCheckbox = null!;
    private CheckBox _vibrationCheckbox = null!;
    private CheckBox _vibrationDxccCheckbox = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_settings);

        _viewModel = AppHost.Current.SettingsViewModel;
        BindViews();
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
            _viewModel.SaveAsync().GetAwaiter().GetResult();
        }

        base.OnPause();
    }

    private void BindViews()
    {
        _ipAddressValue = FindViewById<TextView>(Resource.Id.ip_address_value)!;
        _portValue = FindViewById<EditText>(Resource.Id.port_value)!;
        _callsignValue = FindViewById<EditText>(Resource.Id.callsign_value)!;
        _locationValue = FindViewById<EditText>(Resource.Id.location_value)!;
        _versionValue = FindViewById<TextView>(Resource.Id.version_value)!;
        _sendNotificationCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_checkbox)!;
        _vibrationCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_checkbox)!;
        _sendNotificationAllCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_all_checkbox)!;
        _vibrationAllCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_all_checkbox)!;
        _sendNotificationDxccCheckbox = FindViewById<CheckBox>(Resource.Id.send_notification_dxcc_checkbox)!;
        _vibrationDxccCheckbox = FindViewById<CheckBox>(Resource.Id.vibration_dxcc_checkbox)!;
        _resetDatabaseButton = FindViewById<Button>(Resource.Id.reset_database)!;
        _resetAllButton = FindViewById<Button>(Resource.Id.reset_all)!;
        _openLogButton = FindViewById<Button>(Resource.Id.open_log)!;
        _addWhitelistButton = FindViewById<Button>(Resource.Id.add_white_list)!;
        _addBackgroundButton = FindViewById<Button>(Resource.Id.add_background)!;
        _setDxccButton = FindViewById<Button>(Resource.Id.set_dxcc)!;
    }

    private void BindEvents()
    {
        _portValue.TextChanged += (_, _) =>
        {
            if (!_isBinding)
            {
                _viewModel.Port = _portValue.Text ?? string.Empty;
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
        {
            if (!_isBinding)
            {
                _viewModel.NotifyOnMyCall = args.IsChecked;
            }
        };

        _vibrationCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _viewModel.VibrateOnMyCall = args.IsChecked;
            }
        };

        _sendNotificationAllCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _viewModel.NotifyOnAnyMessage = args.IsChecked;
            }
        };

        _vibrationAllCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _viewModel.VibrateOnAnyMessage = args.IsChecked;
            }
        };

        _sendNotificationDxccCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _viewModel.NotifyOnSelectedDxcc = args.IsChecked;
            }
        };

        _vibrationDxccCheckbox.CheckedChange += (_, args) =>
        {
            if (!_isBinding)
            {
                _viewModel.VibrateOnSelectedDxcc = args.IsChecked;
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

        _setDxccButton.Click += (_, _) => StartActivity(typeof(DxccSelectionActivity));
    }

    private async Task LoadAsync()
    {
        _isBinding = true;
        await _viewModel.LoadAsync().ConfigureAwait(false);
        RunOnUiThread(() =>
        {
            _ipAddressValue.Text = string.IsNullOrWhiteSpace(_viewModel.LocalIpAddress)
                ? GetString(Resource.String.no_wifi)
                : _viewModel.LocalIpAddress;
            _portValue.Text = _viewModel.Port;
            _callsignValue.Text = _viewModel.MyCallsign;
            _locationValue.Text = _viewModel.MyGrid;
            _versionValue.Text = _viewModel.VersionName;
            _sendNotificationCheckbox.Checked = _viewModel.NotifyOnMyCall;
            _vibrationCheckbox.Checked = _viewModel.VibrateOnMyCall;
            _sendNotificationAllCheckbox.Checked = _viewModel.NotifyOnAnyMessage;
            _vibrationAllCheckbox.Checked = _viewModel.VibrateOnAnyMessage;
            _sendNotificationDxccCheckbox.Checked = _viewModel.NotifyOnSelectedDxcc;
            _vibrationDxccCheckbox.Checked = _viewModel.VibrateOnSelectedDxcc;
            _addWhitelistButton.Enabled = !_viewModel.IsIgnoringBatteryOptimizations;
            _setDxccButton.Text = $"{GetString(Resource.String.set_dxcc_entity)} ({_viewModel.SelectedDxccCount})";
            _isBinding = false;
        });
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
            dialog.Show();
        });
        return tcs.Task;
    }

    private void ShowBackgroundHelpDialog()
    {
        var builder = new AlertDialog.Builder(this);
        builder.SetTitle(Resource.String.background_help_title);
        builder.SetMessage(Resource.String.background_help_message);
        builder.SetPositiveButton(Android.Resource.String.Ok, (_, _) => { });
        builder.Show();
    }
}
