using System.Collections.Specialized;
using System.ComponentModel;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Views.Animations;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using Serilog;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Core.Services;
using WsjtxWatcher.Core.Utilities;
using WsjtxWatcher.Core.ViewModels;
using WsjtxWatcher.UI.Adapters;
using WsjtxWatcher.UI.Services;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/app_name", MainLauncher = true, Exported = true, LaunchMode = LaunchMode.SingleTop)]
public sealed class MainActivity : LocalizedActivity
{
    public const string ScrollToBottomFromNotificationExtra = "wsjtxwatcher.scroll_to_bottom_from_notification";
    private const int AutoScrollThresholdItems = 6;

    private MainViewModel _viewModel = null!;
    private DecodedMessageAdapter _adapter = null!;
    private TextView _aboutMe = null!;
    private EditText _callsignSearch = null!;
    private int _dragTouchSlop;
    private float _jumpButtonStartRawX;
    private float _jumpButtonStartRawY;
    private float _jumpButtonStartX;
    private float _jumpButtonStartY;
    private bool _isDraggingJumpButton;
    private ImageButton _jumpToBottomButton = null!;
    private RecyclerView _listView = null!;
    private LinearLayoutManager _listLayoutManager = null!;
    private IMenuItem? _startServerMenuItem;
    private IMenuItem? _stopServerMenuItem;
    private TextView _totalRecord = null!;
    private Animation _transmitBlinkAnimation = null!;
    private RelativeLayout _transmitLayout = null!;
    private TextView _transmitMessage = null!;
    private bool _isStoppingService;
    private bool _pendingScrollToBottom;
    private INotificationService _notificationService = null!;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_main);
        Window?.AddFlags(WindowManagerFlags.KeepScreenOn);

        _viewModel = AppHost.Current.GetRequiredService<MainViewModel>();
        _notificationService = AppHost.Current.GetRequiredService<INotificationService>();
        BindViews();
        BindViewModel();
        HandleLaunchIntent(Intent);
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
                _ = StopWatcherServiceAsync();
                return true;
            case Resource.Id.clear_record:
                _viewModel.ClearMessages();
                return true;
            default:
                return item is not null && base.OnMenuItemSelected(featureId, item);
        }
    }

    protected override void OnResume()
    {
        base.OnResume();
        MaybeScrollToBottom();
        _ = RefreshAsync();
    }

    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandleLaunchIntent(intent);
        MaybeScrollToBottom();
    }

    protected override void OnDestroy()
    {
        if (_viewModel is not null)
        {
            _viewModel.State.PropertyChanged -= OnStatePropertyChanged;
            _viewModel.Messages.CollectionChanged -= OnMessagesCollectionChanged;
        }

        base.OnDestroy();
    }

    private async Task InitializeAsync()
    {
        await _viewModel.InitializeAsync().ConfigureAwait(false);
        RunOnUiThread(() =>
        {
            Render();
            MaybeScrollToBottom();
        });
    }

    private async Task RefreshAsync()
    {
        await _viewModel.RefreshSettingsAsync().ConfigureAwait(false);
        RunOnUiThread(() =>
        {
            _adapter.NotifyDataSetChanged();
            Render();
            MaybeScrollToBottom();
        });
    }

    private void BindViews()
    {
        _listView = FindViewById<RecyclerView>(Resource.Id.calllist_view)!;
        _aboutMe = FindViewById<TextView>(Resource.Id.about_me)!;
        _totalRecord = FindViewById<TextView>(Resource.Id.total_record)!;
        _callsignSearch = FindViewById<EditText>(Resource.Id.callsign_search)!;
        _jumpToBottomButton = FindViewById<ImageButton>(Resource.Id.jump_to_bottom_button)!;
        _transmitLayout = FindViewById<RelativeLayout>(Resource.Id.transmittingLayout)!;
        _transmitMessage = FindViewById<TextView>(Resource.Id.transmittingMessageTextView)!;

        _adapter = new DecodedMessageAdapter(this, _viewModel.Messages, () => _viewModel.SettingsSnapshot, OnMessageItemLongClick);
        _listLayoutManager = new LinearLayoutManager(this);
        _listView.HasFixedSize = true;
        _listView.SetLayoutManager(_listLayoutManager);
        _listView.SetAdapter(_adapter);
        _listView.SetItemAnimator(null);
        _callsignSearch.TextChanged += (_, _) => _adapter.ApplyFilter(_callsignSearch.Text ?? string.Empty);
        _jumpToBottomButton.Click += (_, _) => ScrollToBottom();
        _dragTouchSlop = ViewConfiguration.Get(this)?.ScaledTouchSlop ?? 8;
        _jumpToBottomButton.Touch += OnJumpToBottomButtonTouch;

        _transmitBlinkAnimation = AnimationUtils.LoadAnimation(this, Resource.Animation.view_blink)!;
    }

    private void BindViewModel()
    {
        _viewModel.State.PropertyChanged += OnStatePropertyChanged;
        _viewModel.Messages.CollectionChanged += OnMessagesCollectionChanged;
    }

    private void OnStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RunOnUiThread(() =>
        {
            if (string.Equals(e.PropertyName, nameof(_viewModel.State.MessagePresentationVersion), StringComparison.Ordinal))
            {
                _adapter.NotifyDataSetChanged();
            }

            Render();
        });
    }

    private void OnMessagesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        var newItemCount = e.NewItems?.Count ?? 0;
        if (e.Action != NotifyCollectionChangedAction.Add || newItemCount <= 0)
        {
            return;
        }

        if (!ShouldAutoScrollForNewMessages(newItemCount))
        {
            return;
        }

        RunOnUiThread(ScrollToBottom);
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

    private void SetBanner(string text, bool visible, bool animate = true)
    {
        _transmitMessage.Text = text;
        if (visible && animate)
        {
            if (_transmitMessage.Animation is null)
            {
                _transmitMessage.StartAnimation(_transmitBlinkAnimation);
            }
        }
        else
        {
            _transmitMessage.ClearAnimation();
        }

        _transmitLayout.Visibility = visible ? ViewStates.Visible : ViewStates.Gone;
    }

    private void UpdateMenuState()
    {
        if (_startServerMenuItem is null || _stopServerMenuItem is null)
        {
            return;
        }

        var isRunning = _viewModel.State.IsServiceRunning;
        _startServerMenuItem.SetEnabled(!isRunning && !_isStoppingService);
        _stopServerMenuItem.SetEnabled(isRunning && !_isStoppingService);
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

    private async Task StopWatcherServiceAsync()
    {
        if (_isStoppingService)
        {
            return;
        }

        _isStoppingService = true;
        RunOnUiThread(UpdateMenuState);
        try
        {
            await Task.Run(async () =>
            {
                await AppHost.Current.GetRequiredService<WatcherController>().StopAsync().ConfigureAwait(false);
            }).ConfigureAwait(false);

            RunOnUiThread(() => StopService(new Intent(this, typeof(MsgPushService))));
        }
        catch (Exception exception)
        {
            Log.Warning(exception, "Failed to stop watcher service.");
            RunOnUiThread(() =>
            {
                Toast.MakeText(this, GetString(Resource.String.stop_service_failed), ToastLength.Short)?.Show();
            });
        }
        finally
        {
            _isStoppingService = false;
            RunOnUiThread(Render);
        }
    }

    private void ScrollToBottom()
    {
        if (_adapter.Count == 0)
        {
            return;
        }

        _listView.Post(() => _listView.ScrollToPosition(_adapter.Count - 1));
    }

    private void OnMessageItemLongClick(int position, View anchorView)
    {
        var message = _adapter[position];
        if (message.IsSystemNotice || message.IsUserTransmit)
        {
            return;
        }

        var band = IgnoredCallsignMatcher.NormalizeBand(RadioBandUtility.GetBandName(message.DialFrequencyHz));
        if (string.IsNullOrWhiteSpace(band))
        {
            Toast.MakeText(this, Resource.String.ignored_callsign_unknown_band, ToastLength.Short)?.Show();
            return;
        }

        var candidates = new List<string>();
        AddIgnoreCandidate(candidates, message.Receiver);
        AddIgnoreCandidate(candidates, message.Transmitter);
        if (candidates.Count == 0)
        {
            return;
        }

        var popupMenu = new PopupMenu(this, anchorView);
        for (var index = 0; index < candidates.Count; index++)
        {
            popupMenu.Menu?.Add(0, index, index, string.Format(GetString(Resource.String.ignore_callsign_on_band), band, candidates[index]));
        }

        popupMenu.MenuItemClick += async (_, args) =>
        {
            var itemId = args.Item?.ItemId ?? -1;
            if (itemId < 0 || itemId >= candidates.Count)
            {
                return;
            }

            var ignoredCallsign = candidates[itemId];
            var added = await AppHost.Current.GetRequiredService<IgnoredCallsignViewModel>()
                .AddAsync(ignoredCallsign, band)
                .ConfigureAwait(false);

            RunOnUiThread(() =>
            {
                Toast.MakeText(
                    this,
                    added
                        ? string.Format(GetString(Resource.String.ignored_callsign_added), band, ignoredCallsign)
                        : GetString(Resource.String.duplicate_ignored_callsign),
                    ToastLength.Short)?.Show();
                _adapter.NotifyDataSetChanged();
            });
        };

        popupMenu.Show();
    }

    private void HandleLaunchIntent(Intent? intent)
    {
        if (intent?.GetBooleanExtra(ScrollToBottomFromNotificationExtra, false) == true)
        {
            _pendingScrollToBottom = true;
            intent.RemoveExtra(ScrollToBottomFromNotificationExtra);
        }
    }

    private void MaybeScrollToBottom()
    {
        if (!_pendingScrollToBottom || _adapter.Count == 0)
        {
            return;
        }

        _pendingScrollToBottom = false;
        ScrollToBottom();
    }

    private static void AddIgnoreCandidate(ICollection<string> candidates, string? callsign)
    {
        var normalized = IgnoredCallsignMatcher.NormalizeCallsign(callsign);
        if (string.IsNullOrWhiteSpace(normalized) || candidates.Contains(normalized))
        {
            return;
        }

        candidates.Add(normalized);
    }

    private bool ShouldAutoScrollForNewMessages(int newItemCount)
    {
        if (!string.IsNullOrWhiteSpace(_callsignSearch.Text))
        {
            return false;
        }

        var previousCount = Math.Max(0, _adapter.Count - Math.Max(0, newItemCount));
        if (previousCount == 0)
        {
            return true;
        }

        var lastVisible = _listLayoutManager.FindLastVisibleItemPosition();
        if (lastVisible == RecyclerView.NoPosition)
        {
            return true;
        }

        return lastVisible >= previousCount - 1 - AutoScrollThresholdItems;
    }

    private void OnJumpToBottomButtonTouch(object? sender, View.TouchEventArgs e)
    {
        if (sender is not View view || e.Event is null)
        {
            e.Handled = false;
            return;
        }

        switch (e.Event.ActionMasked)
        {
            case MotionEventActions.Down:
                _isDraggingJumpButton = false;
                _jumpButtonStartRawX = e.Event.RawX;
                _jumpButtonStartRawY = e.Event.RawY;
                _jumpButtonStartX = view.GetX();
                _jumpButtonStartY = view.GetY();
                e.Handled = true;
                return;

            case MotionEventActions.Move:
                var deltaX = e.Event.RawX - _jumpButtonStartRawX;
                var deltaY = e.Event.RawY - _jumpButtonStartRawY;
                if (!_isDraggingJumpButton &&
                    (Math.Abs(deltaX) > _dragTouchSlop || Math.Abs(deltaY) > _dragTouchSlop))
                {
                    _isDraggingJumpButton = true;
                }

                if (_isDraggingJumpButton && view.Parent is View parentView)
                {
                    var maxX = Math.Max(0, parentView.Width - view.Width);
                    var maxY = Math.Max(0, parentView.Height - view.Height);
                    view.SetX(Math.Clamp(_jumpButtonStartX + deltaX, 0f, maxX));
                    view.SetY(Math.Clamp(_jumpButtonStartY + deltaY, 0f, maxY));
                }

                e.Handled = true;
                return;

            case MotionEventActions.Up:
                if (!_isDraggingJumpButton)
                {
                    view.PerformClick();
                }

                _isDraggingJumpButton = false;
                e.Handled = true;
                return;

            case MotionEventActions.Cancel:
                _isDraggingJumpButton = false;
                e.Handled = true;
                return;
        }

        e.Handled = false;
    }
}
