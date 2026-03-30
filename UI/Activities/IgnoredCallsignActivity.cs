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
    private EditText _bandValue = null!;
    private EditText _callsignValue = null!;
    private TextView _emptyText = null!;
    private LinearLayout _entryList = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_ignored_callsigns);

        _viewModel = AppHost.Current.GetRequiredService<IgnoredCallsignViewModel>();
        BindViews();
        BindEvents();
        _ = LoadAsync();
    }

    private void BindViews()
    {
        _callsignValue = FindViewById<EditText>(Resource.Id.ignored_callsign_value)!;
        _bandValue = FindViewById<EditText>(Resource.Id.ignored_callsign_band_value)!;
        _addButton = FindViewById<Button>(Resource.Id.add_ignored_callsign)!;
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

                _callsignValue.Text = string.Empty;
                _bandValue.Text = string.Empty;
                _entries.Add(new IgnoredCallsignEntry
                {
                    Callsign = callsign,
                    Band = band
                });
                SortEntries();
                RenderEntries();
            });
        };
    }

    private async Task LoadAsync()
    {
        var entries = await _viewModel.LoadAsync().ConfigureAwait(false);
        _entries.Clear();
        _entries.AddRange(entries);
        RunOnUiThread(RenderEntries);
    }

    private void RenderEntries()
    {
        SortEntries();
        _entryList.RemoveAllViews();
        _emptyText.Visibility = _entries.Count == 0 ? ViewStates.Visible : ViewStates.Gone;

        foreach (var entry in _entries)
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

    private void SortEntries()
    {
        _entries.Sort((left, right) =>
        {
            var bandComparison = string.Compare(left.Band, right.Band, StringComparison.OrdinalIgnoreCase);
            return bandComparison != 0
                ? bandComparison
                : string.Compare(left.Callsign, right.Callsign, StringComparison.OrdinalIgnoreCase);
        });
    }

    private int Dp(int value)
    {
        return (int)(value * Resources?.DisplayMetrics?.Density ?? value);
    }
}
