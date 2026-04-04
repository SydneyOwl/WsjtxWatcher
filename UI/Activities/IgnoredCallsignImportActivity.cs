using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.ViewModels;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/import_ignored_callsigns", Exported = false)]
public sealed class IgnoredCallsignImportActivity : LocalizedActivity
{
    private static readonly ImportLookbackOption[] LookbackOptions =
    [
        new("30", 30),
        new("90", 90),
        new("180", 180),
        new("1y", 365),
        new("3y", 365 * 3),
        new("5y", 365 * 5),
        new("all", 365 * 100)
    ];

    private ICloudlogIgnoredCallsignImportService _importService = null!;
    private IgnoredCallsignViewModel _ignoredCallsignViewModel = null!;
    private Spinner _lookbackDaysValue = null!;
    private EditText _passwordValue = null!;
    private ProgressBar _progressBar = null!;
    private Button _importButton = null!;
    private EditText _stationIdValue = null!;
    private TextView _statusView = null!;
    private EditText _urlValue = null!;
    private EditText _usernameValue = null!;
    private bool _isBusy;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_ignored_callsign_import);

        _importService = AppHost.Current.GetRequiredService<ICloudlogIgnoredCallsignImportService>();
        _ignoredCallsignViewModel = AppHost.Current.GetRequiredService<IgnoredCallsignViewModel>();

        BindViews();
        BindEvents();
        InitializeState();
    }

    private void BindViews()
    {
        _urlValue = FindViewById<EditText>(Resource.Id.cloudlog_url_value)!;
        _stationIdValue = FindViewById<EditText>(Resource.Id.cloudlog_station_id_value)!;
        _usernameValue = FindViewById<EditText>(Resource.Id.cloudlog_username_value)!;
        _passwordValue = FindViewById<EditText>(Resource.Id.cloudlog_password_value)!;
        _lookbackDaysValue = FindViewById<Spinner>(Resource.Id.cloudlog_lookback_days_value)!;
        _statusView = FindViewById<TextView>(Resource.Id.cloudlog_import_status)!;
        _progressBar = FindViewById<ProgressBar>(Resource.Id.cloudlog_import_progress)!;
        _importButton = FindViewById<Button>(Resource.Id.import_cloudlog_ignored_callsigns)!;
    }

    private void BindEvents()
    {
        _importButton.Click += async (_, _) => await ImportAsync().ConfigureAwait(false);
    }

    private void InitializeState()
    {
        var adapter = new ArrayAdapter<string>(
            this,
            Android.Resource.Layout.SimpleSpinnerItem,
            LookbackOptions.Select(option => ResolveLookbackLabel(option.Label)).ToArray());
        adapter.SetDropDownViewResource(Android.Resource.Layout.SimpleSpinnerDropDownItem);
        _lookbackDaysValue.Adapter = adapter;
        _lookbackDaysValue.SetSelection(LookbackOptions.Length - 1);
        _statusView.Text = GetString(Resource.String.cloudlog_import_status_idle);
        SetBusy(false);
    }

    private async Task ImportAsync()
    {
        if (_isBusy)
        {
            return;
        }

        var lookbackDays = ResolveLookbackDays();

        var stationId = (_stationIdValue.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(stationId))
        {
            Toast.MakeText(this, Resource.String.cloudlog_import_station_required, ToastLength.Short)?.Show();
            return;
        }

        RunOnUiThread(() =>
        {
            SetBusy(true);
            _statusView.Text = GetString(Resource.String.cloudlog_import_importing);
        });

        try
        {
            var importedData = await _importService
                .DownloadIgnoredCallsignEntriesAsync(
                    _urlValue.Text ?? string.Empty,
                    _usernameValue.Text ?? string.Empty,
                    _passwordValue.Text ?? string.Empty,
                    stationId,
                    lookbackDays)
                .ConfigureAwait(false);

            if (importedData.RecordCount == 0)
            {
                RunOnUiThread(() =>
                {
                    _statusView.Text = GetString(Resource.String.cloudlog_import_no_qso_records);
                    Toast.MakeText(this, Resource.String.cloudlog_import_no_qso_records, ToastLength.Long)?.Show();
                });
                return;
            }

            var mergeResult = await _ignoredCallsignViewModel.MergeAsync(importedData.Entries).ConfigureAwait(false);
            RunOnUiThread(() =>
            {
                if (mergeResult.CandidateCount == 0)
                {
                    _statusView.Text = GetString(Resource.String.cloudlog_import_no_valid_entries);
                    Toast.MakeText(this, Resource.String.cloudlog_import_no_valid_entries, ToastLength.Long)?.Show();
                    return;
                }

                var message = GetString(
                    Resource.String.cloudlog_import_completed,
                    mergeResult.AddedCount,
                    mergeResult.DuplicateCount,
                    mergeResult.CandidateCount);
                _statusView.Text = message;
                Toast.MakeText(this, message, ToastLength.Long)?.Show();
                Finish();
            });
        }
        catch (Exception exception)
        {
            RunOnUiThread(() =>
            {
                _statusView.Text = exception.Message;
                Toast.MakeText(this, exception.Message, ToastLength.Long)?.Show();
            });
        }
        finally
        {
            RunOnUiThread(() => SetBusy(false));
        }
    }

    private void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        _progressBar.Visibility = isBusy ? ViewStates.Visible : ViewStates.Gone;
        _importButton.Enabled = !isBusy;
        _lookbackDaysValue.Enabled = !isBusy;
    }

    private int ResolveLookbackDays()
    {
        var position = _lookbackDaysValue.SelectedItemPosition;
        return position >= 0 && position < LookbackOptions.Length
            ? LookbackOptions[position].Days
            : LookbackOptions[^1].Days;
    }

    private string ResolveLookbackLabel(string label)
    {
        return string.Equals(label, "all", StringComparison.OrdinalIgnoreCase)
            ? GetString(Resource.String.cloudlog_lookback_all)
            : label;
    }

    private sealed record ImportLookbackOption(string Label, int Days);
}
