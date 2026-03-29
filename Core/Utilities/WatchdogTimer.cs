namespace WsjtxWatcher.Core.Utilities;

public sealed class WatchdogTimer : IDisposable
{
    private readonly TimeSpan _timeout;
    private readonly Action<bool> _timeoutChanged;
    private readonly Timer _timer;
    private DateTimeOffset _lastFeedUtc;
    private bool _isTimedOut;

    public WatchdogTimer(TimeSpan timeout, Action<bool> timeoutChanged)
    {
        _timeout = timeout;
        _timeoutChanged = timeoutChanged;
        _lastFeedUtc = DateTimeOffset.UtcNow;
        _timer = new Timer(OnTick, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    public void Start()
    {
        _lastFeedUtc = DateTimeOffset.UtcNow;
        _isTimedOut = false;
        _timer.Change(_timeout, _timeout);
    }

    public void Stop()
    {
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        _isTimedOut = false;
    }

    public void Feed()
    {
        _lastFeedUtc = DateTimeOffset.UtcNow;
        if (_isTimedOut)
        {
            _isTimedOut = false;
            _timeoutChanged(false);
        }
    }

    private void OnTick(object? state)
    {
        if (_isTimedOut)
        {
            return;
        }

        if (DateTimeOffset.UtcNow - _lastFeedUtc > _timeout)
        {
            _isTimedOut = true;
            _timeoutChanged(true);
        }
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
