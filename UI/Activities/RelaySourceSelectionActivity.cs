using System.Collections.Specialized;
using System.ComponentModel;
using Android.App;
using Android.OS;
using Android.Widget;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.ViewModels;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/relay_source_picker_title", Exported = false)]
public sealed class RelaySourceSelectionActivity : LocalizedActivity
{
    private RelaySourceSelectionViewModel _viewModel = null!;
    private TextView _statusView = null!;
    private Button _refreshButton = null!;
    private ListView _listView = null!;
    private TextView _emptyView = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_relay_sources);

        _viewModel = AppHost.Current.GetRequiredService<RelaySourceSelectionViewModel>();
        _statusView = FindViewById<TextView>(Resource.Id.relay_source_page_status)!;
        _refreshButton = FindViewById<Button>(Resource.Id.relay_source_page_refresh)!;
        _listView = FindViewById<ListView>(Resource.Id.relay_source_list)!;
        _emptyView = FindViewById<TextView>(Resource.Id.relay_source_empty)!;

        _viewModel.RelayRuntimeState.PropertyChanged += OnRelayRuntimeStateChanged;
        _viewModel.RelayRuntimeState.Sources.CollectionChanged += OnRelaySourcesChanged;
        _refreshButton.Click += async (_, _) => await _viewModel.RefreshAsync().ConfigureAwait(false);
        _listView.ItemClick += async (_, args) =>
        {
            if (args.Position < 0 || args.Position >= _viewModel.RelayRuntimeState.Sources.Count)
            {
                return;
            }

            var selectedSource = _viewModel.RelayRuntimeState.Sources[args.Position];
            await _viewModel.SelectAsync(selectedSource.SourceName).ConfigureAwait(false);
            RunOnUiThread(() =>
            {
                Toast.MakeText(this, Resource.String.relay_source_saved, ToastLength.Short)?.Show();
                Finish();
            });
        };

        Render();
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

    private void OnRelayRuntimeStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        RunOnUiThread(Render);
    }

    private void OnRelaySourcesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RunOnUiThread(Render);
    }

    private void Render()
    {
        var runtimeState = _viewModel.RelayRuntimeState;
        var status = string.IsNullOrWhiteSpace(runtimeState.ConnectionStatus)
            ? GetString(Resource.String.wait_conn)
            : runtimeState.ConnectionStatus;
        _statusView.Text = string.Format(GetString(Resource.String.relay_source_page_status), status);

        var rows = runtimeState.Sources
            .Select(source => string.Format(
                GetString(Resource.String.relay_source_status_format),
                source.DisplayName,
                GetString(source.Online ? Resource.String.relay_source_status_online : Resource.String.relay_source_status_offline)))
            .ToArray();

        _listView.Adapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1, rows);
        var hasItems = rows.Length > 0;
        _emptyView.Visibility = hasItems ? Android.Views.ViewStates.Gone : Android.Views.ViewStates.Visible;
        _listView.Visibility = hasItems ? Android.Views.ViewStates.Visible : Android.Views.ViewStates.Gone;
    }
}
