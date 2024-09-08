public class Watchdog : IDisposable
{
    private readonly TimeSpan _feedInterval;
    private readonly Action<bool> _timeoutCallback;
    private readonly Timer _timer;
    private bool _hasTimedOut;
    private DateTime _lastFedTime;

    /// <summary>
    ///     构造函数，初始化看门狗。
    /// </summary>
    /// <param name="feedInterval">看门狗的喂食间隔。</param>
    /// <param name="timeoutCallback">超时回调函数，接受一个 bool 参数。</param>
    public Watchdog(TimeSpan feedInterval, Action<bool> timeoutCallback)
    {
        _feedInterval = feedInterval;
        _timeoutCallback = timeoutCallback ?? throw new ArgumentNullException(nameof(timeoutCallback));
        _lastFedTime = DateTime.Now;
        _hasTimedOut = false;

        // 初始化时不启动定时器
        _timer = new Timer(CheckForTimeout, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
    }

    /// <summary>
    ///     释放资源。
    /// </summary>
    public void Dispose()
    {
        _timer?.Dispose();
    }

    /// <summary>
    ///     启动看门狗定时器。
    /// </summary>
    public void Start()
    {
        _lastFedTime = DateTime.Now; // 启动时更新喂食时间
        _timer.Change(_feedInterval, _feedInterval); // 设置定时器间隔
    }

    /// <summary>
    ///     停止看门狗定时器。
    /// </summary>
    public void Stop()
    {
        _timer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan); // 停止定时器
    }

    /// <summary>
    ///     喂看门狗，更新最后喂食时间。
    /// </summary>
    public void Feed()
    {
        ResetFeedTime();
    }

    /// <summary>
    ///     重置喂食时间为当前时间。
    /// </summary>
    public void ResetFeedTime()
    {
        _lastFedTime = DateTime.Now;
        _hasTimedOut = false; // 重置超时标志
    }

    /// <summary>
    ///     检查是否超时，并触发回调。
    /// </summary>
    private void CheckForTimeout(object state)
    {
        if (DateTime.Now - _lastFedTime > _feedInterval)
        {
            if (!_hasTimedOut)
            {
                _hasTimedOut = true;
                _timeoutCallback?.Invoke(true); // 超时且没有新的喂食
            }
        }
        else
        {
            if (_hasTimedOut)
            {
                _timeoutCallback?.Invoke(false); // 超时后有新的喂食
                _hasTimedOut = false; // 重置超时标志
            }
        }
    }
}