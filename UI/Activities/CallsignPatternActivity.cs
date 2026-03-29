using Android.App;
using Android.OS;
using Android.Views;
using Android.Widget;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.Utilities;
using WsjtxWatcher.Core.ViewModels;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/specified_callsign_patterns", Exported = false)]
public sealed class CallsignPatternActivity : LocalizedActivity
{
    private readonly List<string> _patterns = [];
    private CallsignPatternViewModel _viewModel = null!;
    private Button _addButton = null!;
    private TextView _emptyText = null!;
    private LinearLayout _patternList = null!;
    private EditText _patternValue = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_callsign_patterns);

        _viewModel = AppHost.Current.GetRequiredService<CallsignPatternViewModel>();
        BindViews();
        BindEvents();
        _ = LoadAsync();
    }

    private void BindViews()
    {
        _patternValue = FindViewById<EditText>(Resource.Id.callsign_pattern_value)!;
        _addButton = FindViewById<Button>(Resource.Id.add_callsign_pattern)!;
        _emptyText = FindViewById<TextView>(Resource.Id.empty_callsign_patterns)!;
        _patternList = FindViewById<LinearLayout>(Resource.Id.callsign_pattern_list)!;
    }

    private void BindEvents()
    {
        _addButton.Click += async (_, _) =>
        {
            var pattern = (_patternValue.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(pattern))
            {
                Toast.MakeText(this, Resource.String.empty_callsign_pattern, ToastLength.Short)?.Show();
                return;
            }

            if (!CallsignPatternMatcher.IsValidPattern(pattern))
            {
                Toast.MakeText(this, Resource.String.invalid_callsign_pattern, ToastLength.Short)?.Show();
                return;
            }

            if (_patterns.Contains(pattern, StringComparer.Ordinal))
            {
                Toast.MakeText(this, Resource.String.duplicate_callsign_pattern, ToastLength.Short)?.Show();
                return;
            }

            _patterns.Add(pattern);
            await _viewModel.SaveAsync(_patterns).ConfigureAwait(false);
            RunOnUiThread(() =>
            {
                _patternValue.Text = string.Empty;
                RenderPatterns();
            });
        };
    }

    private async Task LoadAsync()
    {
        var patterns = await _viewModel.LoadAsync().ConfigureAwait(false);
        _patterns.Clear();
        _patterns.AddRange(patterns);
        RunOnUiThread(RenderPatterns);
    }

    private void RenderPatterns()
    {
        _patternList.RemoveAllViews();
        _emptyText.Visibility = _patterns.Count == 0 ? ViewStates.Visible : ViewStates.Gone;

        foreach (var pattern in _patterns)
        {
            _patternList.AddView(CreatePatternRow(pattern));
        }
    }

    private View CreatePatternRow(string pattern)
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
        textView.Text = pattern;
        textView.SetTextIsSelectable(true);
        textView.TextSize = 14f;

        var deleteButton = new Button(this);
        deleteButton.LayoutParameters = new LinearLayout.LayoutParams(
            ViewGroup.LayoutParams.WrapContent,
            ViewGroup.LayoutParams.WrapContent);
        deleteButton.Text = GetString(Resource.String.delete_pattern);
        deleteButton.Click += async (_, _) =>
        {
            _patterns.Remove(pattern);
            await _viewModel.SaveAsync(_patterns).ConfigureAwait(false);
            RunOnUiThread(RenderPatterns);
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
