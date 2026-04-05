using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;
using WsjtxWatcher.Core.ViewModels;
using WsjtxWatcher.UI.Adapters;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/ignored_callsigns", Exported = false)]
public sealed class IgnoredCallsignActivity : LocalizedActivity
{
    private const int PageSize = 200;
    private readonly List<IgnoredCallsignEntry> _entries = [];
    private IgnoredCallsignViewModel _viewModel = null!;
    private IgnoredCallsignAdapter _adapter = null!;
    private Button _addButton = null!;
    private Button _clearAllButton = null!;
    private Button _importButton = null!;
    private Button _nextPageButton = null!;
    private Button _previousPageButton = null!;
    private EditText _bandValue = null!;
    private EditText _callsignValue = null!;
    private EditText _searchValue = null!;
    private TextView _emptyText = null!;
    private TextView _pageInfoText = null!;
    private RecyclerView _entryList = null!;
    private int _currentPageIndex;
    private int _loadVersion;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_ignored_callsigns);

        _viewModel = AppHost.Current.GetRequiredService<IgnoredCallsignViewModel>();
        BindViews();
        InitializeEntryList();
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
        _addButton = FindViewById<Button>(Resource.Id.add_ignored_callsign)!;
        _importButton = FindViewById<Button>(Resource.Id.import_ignored_callsigns_button)!;
        _clearAllButton = FindViewById<Button>(Resource.Id.clear_all_ignored_callsigns_button)!;
        _emptyText = FindViewById<TextView>(Resource.Id.empty_ignored_callsigns)!;
        _previousPageButton = FindViewById<Button>(Resource.Id.previous_ignored_callsign_page)!;
        _nextPageButton = FindViewById<Button>(Resource.Id.next_ignored_callsign_page)!;
        _pageInfoText = FindViewById<TextView>(Resource.Id.ignored_callsign_page_info)!;
        _entryList = FindViewById<RecyclerView>(Resource.Id.ignored_callsign_list)!;
    }

    private void InitializeEntryList()
    {
        _adapter = new IgnoredCallsignAdapter(this, DeleteEntryAsync);
        _entryList.SetLayoutManager(new LinearLayoutManager(this));
        _entryList.SetAdapter(_adapter);
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
                if (!added || IsFinishing || IsDestroyed)
                {
                    if (!added)
                    {
                        Toast.MakeText(this, Resource.String.duplicate_ignored_callsign, ToastLength.Short)?.Show();
                    }

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
                RenderEntries(new IgnoredCallsignEntry
                {
                    Callsign = callsign,
                    Band = band
                });
            });
        };

        _searchValue.TextChanged += (_, _) =>
        {
            _currentPageIndex = 0;
            RenderEntries();
        };

        _importButton.Click += (_, _) => StartActivity(typeof(IgnoredCallsignImportActivity));
        _clearAllButton.Click += async (_, _) =>
        {
            if (!await ConfirmClearAllAsync().ConfigureAwait(false))
            {
                return;
            }

            var clearVersion = Interlocked.Increment(ref _loadVersion);
            await _viewModel.ClearAsync().ConfigureAwait(false);
            RunOnUiThread(() =>
            {
                if (IsFinishing || IsDestroyed || clearVersion != Volatile.Read(ref _loadVersion))
                {
                    return;
                }

                _entries.Clear();
                _currentPageIndex = 0;
                RenderEntries();
            });
        };
        _previousPageButton.Click += (_, _) =>
        {
            if (_currentPageIndex <= 0)
            {
                return;
            }

            _currentPageIndex--;
            RenderEntries();
        };
        _nextPageButton.Click += (_, _) =>
        {
            _currentPageIndex++;
            RenderEntries();
        };
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
            _currentPageIndex = 0;
            RenderEntries();
        });
    }

    private void RenderEntries(IgnoredCallsignEntry? focusEntry = null)
    {
        var keyword = IgnoredCallsignMatcher.NormalizeCallsign(_searchValue.Text);
        var visibleEntries = _entries
            .Where(entry =>
                string.IsNullOrWhiteSpace(keyword) ||
                entry.Callsign.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.Band, StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Callsign, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (focusEntry is not null)
        {
            var focusIndex = visibleEntries.FindIndex(entry =>
                string.Equals(entry.Callsign, focusEntry.Callsign, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(entry.Band, focusEntry.Band, StringComparison.OrdinalIgnoreCase));
            if (focusIndex >= 0)
            {
                _currentPageIndex = focusIndex / PageSize;
            }
        }

        _emptyText.Visibility = visibleEntries.Count == 0 ? ViewStates.Visible : ViewStates.Gone;
        if (visibleEntries.Count == 0)
        {
            _currentPageIndex = 0;
            _pageInfoText.Text = string.Format(GetString(Resource.String.ignored_callsign_page_status), 0, 0);
            _previousPageButton.Enabled = false;
            _nextPageButton.Enabled = false;
            _adapter.UpdateEntries([]);
            return;
        }

        var totalPages = (visibleEntries.Count + PageSize - 1) / PageSize;
        _currentPageIndex = Math.Clamp(_currentPageIndex, 0, totalPages - 1);
        var pageEntries = visibleEntries
            .Skip(_currentPageIndex * PageSize)
            .Take(PageSize)
            .ToList();

        _pageInfoText.Text = string.Format(GetString(Resource.String.ignored_callsign_page_status), _currentPageIndex + 1, totalPages);
        _previousPageButton.Enabled = _currentPageIndex > 0;
        _nextPageButton.Enabled = _currentPageIndex < totalPages - 1;
        _adapter.UpdateEntries(pageEntries);
        _entryList.ScrollToPosition(0);
    }

    private async Task DeleteEntryAsync(IgnoredCallsignEntry entry)
    {
        await _viewModel.RemoveAsync(entry).ConfigureAwait(false);
        RunOnUiThread(() =>
        {
            if (IsFinishing || IsDestroyed)
            {
                return;
            }

            Interlocked.Increment(ref _loadVersion);
            _entries.RemoveAll(existing =>
                string.Equals(existing.Callsign, entry.Callsign, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(existing.Band, entry.Band, StringComparison.OrdinalIgnoreCase));
            RenderEntries();
        });
    }

    private Task<bool> ConfirmClearAllAsync()
    {
        var tcs = new TaskCompletionSource<bool>();
        RunOnUiThread(() =>
        {
            var builder = new AlertDialog.Builder(this);
            builder.SetTitle(Resource.String.clear_all_ignored_callsigns);
            builder.SetMessage(Resource.String.clear_all_ignored_callsigns_confirm);
            builder.SetPositiveButton(Android.Resource.String.Ok, (_, _) => tcs.TrySetResult(true));
            builder.SetNegativeButton(Android.Resource.String.Cancel, (_, _) => tcs.TrySetResult(false));
            var dialog = builder.Create() ?? throw new InvalidOperationException("Failed to create clear ignored callsigns dialog.");
            dialog.CancelEvent += (_, _) => tcs.TrySetResult(false);
            dialog.Show();
        });
        return tcs.Task;
    }
}
