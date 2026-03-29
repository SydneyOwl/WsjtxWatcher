using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Services;

namespace WsjtxWatcher.Core.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly WatcherController _watcherController;

    public MainViewModel(WatcherController watcherController)
    {
        _watcherController = watcherController;
        State = watcherController.State;
        State.PropertyChanged += OnStatePropertyChanged;
        Messages = State.Messages;
    }

    public WatcherState State { get; }

    public ObservableCollection<DecodedRadioMessage> Messages { get; }

    public AppSettings SettingsSnapshot => _watcherController.CurrentSettings;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _watcherController.InitializeAsync(cancellationToken).ConfigureAwait(false);
        OnPropertyChanged(nameof(SettingsSnapshot));
    }

    public async Task RefreshSettingsAsync(CancellationToken cancellationToken = default)
    {
        await _watcherController.ReloadSettingsAsync(cancellationToken).ConfigureAwait(false);
        OnPropertyChanged(nameof(SettingsSnapshot));
    }

    public void ClearMessages()
    {
        State.ClearMessages();
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        OnPropertyChanged(e.PropertyName);
    }
}
