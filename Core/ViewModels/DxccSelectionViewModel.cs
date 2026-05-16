using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.ViewModels;

public partial class DxccSelectionViewModel : ObservableObject
{
    private readonly ICountryCatalog _countryCatalog;
    private readonly ISettingsStore _settingsStore;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly Services.WatcherController _watcherController;
    private readonly SemaphoreSlim _selectionLock = new(1, 1);
    private readonly HashSet<int> _selectedIds = [];
    private int _refreshVersion;

    [ObservableProperty]
    private string searchText = string.Empty;

    public DxccSelectionViewModel(
        ICountryCatalog countryCatalog,
        ISettingsStore settingsStore,
        IUiDispatcher uiDispatcher,
        Services.WatcherController watcherController)
    {
        _countryCatalog = countryCatalog;
        _settingsStore = settingsStore;
        _uiDispatcher = uiDispatcher;
        _watcherController = watcherController;
    }

    public ObservableCollection<CountrySelectionItem> Items { get; } = [];

    public bool AreAllDisplayedItemsSelected => Items.Count > 0 && Items.All(item => item.IsSelected);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        await _uiDispatcher.InvokeAsync(() =>
        {
            _selectedIds.Clear();
            _selectedIds.UnionWith(settings.PreferredDxccIds);
            SearchText = string.Empty;
        }).ConfigureAwait(false);

        await RefreshItemsAsync(string.Empty, cancellationToken).ConfigureAwait(false);
    }

    public async Task SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var normalizedQuery = query ?? string.Empty;
        await _uiDispatcher.InvokeAsync(() =>
        {
            SearchText = normalizedQuery;
        }).ConfigureAwait(false);

        await RefreshItemsAsync(normalizedQuery, cancellationToken).ConfigureAwait(false);
    }

    public async Task ToggleAsync(CountrySelectionItem item, bool isSelected, CancellationToken cancellationToken = default)
    {
        await _selectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            HashSet<int> selectedIdsSnapshot = [];
            await _uiDispatcher.InvokeAsync(() =>
            {
                item.IsSelected = isSelected;
                if (isSelected)
                {
                    _selectedIds.Add(item.Country.Id);
                }
                else
                {
                    _selectedIds.Remove(item.Country.Id);
                }

                selectedIdsSnapshot = [.. _selectedIds];
                OnPropertyChanged(nameof(AreAllDisplayedItemsSelected));
            }).ConfigureAwait(false);

            await SaveAsync(selectedIdsSnapshot, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _selectionLock.Release();
        }
    }

    public async Task SetAllDisplayedAsync(bool isSelected, CancellationToken cancellationToken = default)
    {
        await _selectionLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            HashSet<int> selectedIdsSnapshot = [];
            await _uiDispatcher.InvokeAsync(() =>
            {
                foreach (var item in Items)
                {
                    item.IsSelected = isSelected;
                    if (isSelected)
                    {
                        _selectedIds.Add(item.Country.Id);
                    }
                    else
                    {
                        _selectedIds.Remove(item.Country.Id);
                    }
                }

                selectedIdsSnapshot = [.. _selectedIds];
                OnPropertyChanged(nameof(AreAllDisplayedItemsSelected));
            }).ConfigureAwait(false);

            await SaveAsync(selectedIdsSnapshot, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _selectionLock.Release();
        }
    }

    private async Task RefreshItemsAsync(string query, CancellationToken cancellationToken)
    {
        var requestVersion = Interlocked.Increment(ref _refreshVersion);
        var countries = string.IsNullOrWhiteSpace(query)
            ? await _countryCatalog.GetAllCountriesAsync(cancellationToken).ConfigureAwait(false)
            : await _countryCatalog.SearchCountriesAsync(query, cancellationToken).ConfigureAwait(false);

        if (requestVersion != Volatile.Read(ref _refreshVersion))
        {
            return;
        }

        await _uiDispatcher.InvokeAsync(() =>
        {
            if (requestVersion != Volatile.Read(ref _refreshVersion))
            {
                return;
            }

            Items.Clear();
            foreach (var country in countries)
            {
                Items.Add(new CountrySelectionItem
                {
                    Country = country,
                    IsSelected = _selectedIds.Contains(country.Id)
                });
            }

            OnPropertyChanged(nameof(AreAllDisplayedItemsSelected));
        }).ConfigureAwait(false);
    }

    private async Task SaveAsync(HashSet<int> selectedIdsSnapshot, CancellationToken cancellationToken)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        settings.PreferredDxccIds = [.. selectedIdsSnapshot];
        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
    }
}
