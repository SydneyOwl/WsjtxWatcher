using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.Core.ViewModels;

public partial class DxccSelectionViewModel : ObservableObject
{
    private readonly ICountryCatalog _countryCatalog;
    private readonly ISettingsStore _settingsStore;
    private readonly HashSet<int> _selectedIds = new();
    [ObservableProperty]
    private string searchText = string.Empty;

    public DxccSelectionViewModel(ICountryCatalog countryCatalog, ISettingsStore settingsStore)
    {
        _countryCatalog = countryCatalog;
        _settingsStore = settingsStore;
    }

    public ObservableCollection<CountrySelectionItem> Items { get; } = new();

    public bool AreAllDisplayedItemsSelected => Items.Count > 0 && Items.All(item => item.IsSelected);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        _selectedIds.Clear();
        _selectedIds.UnionWith(settings.PreferredDxccIds);
        await RefreshItemsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        SearchText = query ?? string.Empty;
        await RefreshItemsAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task ToggleAsync(CountrySelectionItem item, bool isSelected, CancellationToken cancellationToken = default)
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

        await SaveAsync(cancellationToken).ConfigureAwait(false);
        OnPropertyChanged(nameof(AreAllDisplayedItemsSelected));
    }

    public async Task SetAllDisplayedAsync(bool isSelected, CancellationToken cancellationToken = default)
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

        await SaveAsync(cancellationToken).ConfigureAwait(false);
        OnPropertyChanged(nameof(AreAllDisplayedItemsSelected));
    }

    private async Task RefreshItemsAsync(CancellationToken cancellationToken)
    {
        var countries = string.IsNullOrWhiteSpace(SearchText)
            ? await _countryCatalog.GetAllCountriesAsync(cancellationToken).ConfigureAwait(false)
            : await _countryCatalog.SearchCountriesAsync(SearchText, cancellationToken).ConfigureAwait(false);

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
    }

    private async Task SaveAsync(CancellationToken cancellationToken)
    {
        var settings = await _settingsStore.LoadAsync(cancellationToken).ConfigureAwait(false);
        settings.PreferredDxccIds = new HashSet<int>(_selectedIds);
        await _settingsStore.SaveAsync(settings, cancellationToken).ConfigureAwait(false);
    }
}
