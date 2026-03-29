using System.ComponentModel;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using Serilog;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.ViewModels;
using WsjtxWatcher.UI.Adapters;
using WsjtxWatcher.UI.Services;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/app_name", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTop)]
public sealed class MainActivity : Activity
{
    private MainViewModel _viewModel = null!;
    private DecodedMessageAdapter _adapter = null!;
    private TextView _aboutMe = null!;
    private EditText _callsignSearch = null!;
    private ListView _listView = null!;
    private IMenuItem? _startServerMenuItem;
    private IMenuItem? _stopServerMenuItem;
    private TextView _totalRecord = null!;
    private RelativeLayout _transmitLayout = null!;
    private TextView _transmitMessage = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);

        _viewModel = AppHost.Current.MainViewModel;
        BindViews();
        BindViewModel();
        RequestNotificationPermissionIfNeeded();
        _ = InitializeAsync();
    }

    public override bool OnCreateOptionsMenu(IMenu? menu)
    {
        MenuInflater.Inflate(Resource.Menu.main_menu, menu);
        if (menu is not null)
        {
            _startServerMenuItem = menu.FindItem(Resource.Id.start_server);
            _stopServerMenuItem = menu.FindItem(Resource.Id.stop_server);
            Render();
        }

        return true;
    }

    public override bool OnMenuItemSelected(int featureId, IMenuItem? item)
    {
        switch (item?.ItemId)
        {
            case Resource.Id.settings:
                StartActivity(typeof(SettingsActivity));
                return true;
            case Resource.Id.start_server:
                StartWatcherService();
                return true;
            case Resource.Id.stop_server:
                StopWatcherService();
                return true;
            default:
                return item is not null && base.OnMenuItemSelected(featureId, item);
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        _ = RefreshAsync();
    }

    protected override void OnDestroy()
    {
        if (_viewModel is not null)
        {
            _viewModel.State.PropertyChanged -= OnStatePropertyChanged;
        }

        base.OnDestroy();
    }

    private async Task InitializeAsync()
    {
        await _viewModel.InitializeAsync().ConfigureAwait(false);
        RunOnUiThread(Render);
    }

    private async Task RefreshAsync()
    {
        await _viewModel.RefreshSettingsAsync().ConfigureAwait(false);
        RunOnUiThread(() =>
        {
            _adapter.NotifyDataSetChanged();
            Render();
        });
    }

    private void BindViews()
    {
        _listView = FindViewById<ListView>(Resource.Id.calllist_view)!;
        _aboutMe = FindViewById<TextView>(Resource.Id.about_me)!;
        _totalRecord = FindViewById<TextView>(Resource.Id.total_record)!;
        _callsignSearch = FindViewById<EditText>(Resource.Id.callsign_search)!;
        _transmitLayout = FindViewById<RelativeLayout>(Resource.Id.transmittingLayout)!;
        _transmitMessage = FindViewById<TextView>(Resource.Id.transmittingMessageTextView)!;

        _adapter = new DecodedMessageAdapter(this, _viewModel.Messages, () => _viewModel.SettingsSnapshot);
        _listView.Adapter = _adapter;
        _callsignSearch.TextChanged += (_, _) => _adapter.ApplyFilter(_callsignSearch.Text ?? string.Empty);

        var blinkAnimation = AnimationUtils.LoadAnimation(this, Resource.Animation.view_blink);
        _transmitMessage.StartAnimation(blinkAnimation);
    }

    private void BindViewModel()
    {
        _viewModel.State.PropertyChanged += OnStatePropertyChanged;
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RunOnUiThread(Render);
    }

    private void Render()
    {
        var state = _viewModel.State;
        _totalRecord.Text = $"{GetString(Resource.String.total_record)} {state.TotalMessages}";
        _aboutMe.Text = $"{GetString(Resource.String.about_me)} {state.MessagesAboutMe}";

        if (!state.IsServiceRunning)
        {
            Title = GetString(Resource.String.app_name);
            SetBanner(GetString(Resource.String.open_service), true);
        }
        else if (state.IsTransmitting)
        {
            Title = GetString(Resource.String.app_name);
            SetBanner(string.IsNullOrWhiteSpace(state.TransmitMessage) ? GetString(Resource.String.txing) : state.TransmitMessage, true);
        }
        else if (state.IsTimedOut)
        {
            Title = GetString(Resource.String.recv_timeout);
            SetBanner(GetString(Resource.String.wait_conn), true);
        }
        else if (state.IsWaitingForConnection)
        {
            Title = GetString(Resource.String.receving);
            SetBanner(GetString(Resource.String.wait_conn), true);
        }
        else
        {
            Title = GetString(Resource.String.receving);
            SetBanner(string.Empty, false);
        }

        UpdateMenuState();
    }

    private void SetBanner(string text, bool visible)
    {
        _transmitMessage.Text = text;
        _transmitLayout.Visibility = visible ? ViewStates.Visible : ViewStates.Gone;
    }

    private void UpdateMenuState()
    {
        if (_startServerMenuItem is null || _stopServerMenuItem is null)
        {
            return;
        }

        var isRunning = _viewModel.State.IsServiceRunning;
        _startServerMenuItem.SetEnabled(!isRunning);
        _stopServerMenuItem.SetEnabled(isRunning);
    }

    private void StartWatcherService()
    {
        try
        {
            var serviceIntent = new Intent(this, typeof(MsgPushService));
            if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
            {
                StartForegroundService(serviceIntent);
            }
            else
            {
                StartService(serviceIntent);
            }
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Failed to start watcher service.");
            Toast.MakeText(this, GetString(Resource.String.start_service_failed), ToastLength.Short)?.Show();
        }
    }

    private void StopWatcherService()
    {
        try
        {
            StopService(new Intent(this, typeof(MsgPushService)));
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Failed to stop watcher service.");
            Toast.MakeText(this, GetString(Resource.String.stop_service_failed), ToastLength.Short)?.Show();
        }
    }

    private void RequestNotificationPermissionIfNeeded()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            return;
        }

        if (CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted)
        {
            RequestPermissions(new[] { Manifest.Permission.PostNotifications }, 1001);
        }
    }
}
