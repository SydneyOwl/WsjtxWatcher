using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;
using WsjtxWatcher.Core.ViewModels;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/ignored_callsigns", Exported = false)]
public sealed class IgnoredCallsignActivity : LocalizedActivity
{
    private readonly List<IgnoredCallsignEntry> _entries = [];
    private IgnoredCallsignViewModel _viewModel = null!;
    private Button _addButton = null!;
    private Button _clearSearchButton = null!;
    private Button _importButton = null!;
    private EditText _bandValue = null!;
    private EditText _callsignValue = null!;
    private EditText _searchValue = null!;
    private TextView _emptyText = null!;
    private LinearLayout _entryList = null!;
    private int _loadVersion;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_ignored_callsigns);

        _viewModel = AppHost.Current.GetRequiredService<IgnoredCallsignViewModel>();
        BindViews();
        BindEvents();
    }

    protected override void OnResume()
    {
        base.OnResume();
        _ = LoadAsync();
    }

    protected override void OnPause()
    {
        Interlocked.Increment(ref _loadVersion);
        base.OnPause();
    }

    private void BindViews()
    {
        _callsignValue = FindViewById<EditText>(Resource.Id.ignored_callsign_value)!;
        _bandValue = FindViewById<EditText>(Resource.Id.ignored_callsign_band_value)!;
        _searchValue = FindViewById<EditText>(Resource.Id.ignored_callsign_search_value)!;
        _clearSearchButton = FindViewById<Button>(Resource.Id.clear_ignored_callsign_search)!;
        _addButton = FindViewById<Button>(Resource.Id.add_ignored_callsign)!;
        _importButton = FindViewById<Button>(Resource.Id.import_ignored_callsigns_button)!;
        _emptyText = FindViewById<TextView>(Resource.Id.empty_ignored_callsigns)!;
        _entryList = FindViewById<LinearLayout>(Resource.Id.ignored_callsign_list)!;
    }

    private void BindEvents()
    {
        _addButton.Click += async (_, _) =>
        {
            var callsign = IgnoredCallsignMatcher.NormalizeCallsign(_callsignValue.Text);
            var band = IgnoredCallsignMatcher.NormalizeBand(_bandValue.Text);
            if (string.IsNullOrWhiteSpace(callsign) || string.IsNullOrWhiteSpace(band))
            {
                Toast.MakeText(this, Resource.String.invalid_ignored_callsign, ToastLength.Short)?.Show();
                return;
            }

            if (_entries.Any(entry =>
                    string.Equals(entry.Callsign, callsign, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(entry.Band, band, StringComparison.OrdinalIgnoreCase)))
            {
                Toast.MakeText(this, Resource.String.duplicate_ignored_callsign, ToastLength.Short)?.Show();
                return;
            }

            var added = await _viewModel.AddAsync(callsign, band).ConfigureAwait(false);
            RunOnUiThread(() =>
            {
                if (!added)
                {
                    Toast.MakeText(this, Resource.String.duplicate_ignored_callsign, ToastLength.Short)?.Show();
                    return;
                }

                Interlocked.Increment(ref _loadVersion);
                _callsignValue.Text = string.Empty;
                _bandValue.Text = string.Empty;
                _entries.Add(new IgnoredCallsignEntry
                {
                    Callsign = callsign,
                    Band = band
                });
                RenderEntries();
            });
        };

        _searchValue.TextChanged += (_, _) => RenderEntries();
        _clearSearchButton.Click += (_, _) =>
        {
            _searchValue.Text = string.Empty;
            _searchValue.ClearFocus();
            RenderEntries();
        };

        _importButton.Click += (_, _) => StartActivity(typeof(IgnoredCallsignImportActivity));
    }

    private async Task LoadAsync()
    {
        var loadVersion = Interlocked.Increment(ref _loadVersion);
        var entries = await _viewModel.LoadAsync().ConfigureAwait(false);
        RunOnUiThread(() =>
        {
            if (IsFinishing || IsDestroyed || loadVersion != Volatile.Read(ref _loadVersion))
            {
                return;
            }

            _entries.Clear();
            _entries.AddRange(entries);
            RenderEntries();
        });
    }

    private void RenderEntries()
    {
        _entryList.RemoveAllViews();
        var keyword = IgnoredCallsignMatcher.NormalizeCallsign(_searchValue.Text);
        var visibleEntries = _entries
            .Where(entry =>
                string.IsNullOrWhiteSpace(keyword) ||
                entry.Callsign.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.Band, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Callsign, StringComparer.OrdinalIgnoreCase)
            .ToList();
        _emptyText.Visibility = visibleEntries.Count == 0 ? ViewStates.Visible : ViewStates.Gone;

        foreach (var entry in visibleEntries)
        {
            _entryList.AddView(CreateEntryRow(entry));
        }
    }

    private View CreateEntryRow(IgnoredCallsignEntry entry)
    {
        var row = new LinearLayout(this)
        {
            Orientation = Orientation.Horizontal
        };
        row.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.MatchParent,
            ViewGroup.LayoutParams.WrapContent)
        {
            TopMargin = Dp(8)
        };
        row.SetPadding(Dp(8), Dp(8), Dp(8), Dp(8));

        var textView = new TextView(this);
        textView.LayoutParameters = new LinearLayout.LayoutParams(0, ViewGroup.LayoutParams.WrapContent, 1f);
        textView.Text = GetString(Resource.String.ignored_callsign_item_format, entry.Band, entry.Callsign);
        textView.SetTextIsSelectable(true);
        textView.TextSize = 14f;

        var deleteButton = new Button(this);
        deleteButton.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent);
        deleteButton.Text = GetString(Resource.String.delete_pattern);
        deleteButton.Click += async (_, _) =>
        {
            await _viewModel.RemoveAsync(entry).ConfigureAwait(false);
            RunOnUiThread(() =>
            {
                Interlocked.Increment(ref _loadVersion);
                _entries.RemoveAll(existing =>
                    string.Equals(existing.Callsign, entry.Callsign, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existing.Band, entry.Band, StringComparison.OrdinalIgnoreCase));
                RenderEntries();
            });
        };

        row.AddView(textView);
        row.AddView(deleteButton);
        return row;
    }

    private int Dp(int value)
    {
        return (int)(value * Resources?.DisplayMetrics?.Density ?? value);
    }
}
