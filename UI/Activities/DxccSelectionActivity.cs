using Android.App;
using Android.OS;
using Android.Widget;
using Java.Util;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.ViewModels;
using WsjtxWatcher.UI.Adapters;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/set_dxcc_entity", Exported = false)]
public sealed class DxccSelectionActivity : LocalizedActivity
{
    private CountrySelectionAdapter _adapter = null!;
    private CheckBox _selectAll = null!;
    private DxccSelectionViewModel _viewModel = null!;
    private bool _suppressSelectAllEvents;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_set_dxcc);

        _viewModel = AppHost.Current.GetRequiredService<DxccSelectionViewModel>();

        var listView = FindViewById<ListView>(Resource.Id.lv_data)!;
        _selectAll = FindViewById<CheckBox>(Resource.Id.che_all)!;
        var searchBox = FindViewById<EditText>(Resource.Id.search_edittext)!;

        _adapter = new CountrySelectionAdapter(
            this,
            _viewModel.Items,
            ResolveCurrentLanguageCode(),
            async (item, isChecked) =>
            {
                await _viewModel.ToggleAsync(item, isChecked).ConfigureAwait(false);
                RunOnUiThread(() =>
                {
                    _adapter.NotifyDataSetChanged();
                    UpdateSelectAllState();
                });
            });

        listView.Adapter = _adapter;

        _selectAll.CheckedChange += async (_, args) =>
        {
            if (_suppressSelectAllEvents)
            {
                return;
            }

            await _viewModel.SetAllDisplayedAsync(args.IsChecked).ConfigureAwait(false);
            RunOnUiThread(() =>
            {
                _adapter.NotifyDataSetChanged();
                UpdateSelectAllState();
            });
        };

        searchBox.TextChanged += async (_, _) =>
        {
            await _viewModel.SearchAsync(searchBox.Text ?? string.Empty).ConfigureAwait(false);
            RunOnUiThread(() =>
            {
                _adapter.NotifyDataSetChanged();
                UpdateSelectAllState();
            });
        };

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        await _viewModel.LoadAsync().ConfigureAwait(false);
        RunOnUiThread(() =>
        {
            _adapter.NotifyDataSetChanged();
            UpdateSelectAllState();
        });
    }

    private void UpdateSelectAllState()
    {
        _suppressSelectAllEvents = true;
        _selectAll.Checked = _viewModel.AreAllDisplayedItemsSelected;
        _suppressSelectAllEvents = false;
    }

    private string ResolveCurrentLanguageCode()
    {
        return Resources?.Configuration?.Locales?.Get(0)?.Language
               ?? Locale.Default?.Language
               ?? string.Empty;
    }
}
