using Android.App;
using Microsoft.Extensions.DependencyInjection;

namespace WsjtxWatcher.App;

public sealed class AppHost
{
    private static readonly object SyncRoot = new();
    private static AppHost? _current;

    private AppHost(ServiceProvider services)
    {
        Services = services;
    }

    public static bool IsInitialized
    {
        get
        {
            lock (SyncRoot)
            {
                return _current is not null;
            }
        }
    }

    public static AppHost Current
    {
        get
        {
            lock (SyncRoot)
            {
                return _current ?? throw new InvalidOperationException("AppHost has not been initialized.");
            }
        }
    }

    public static void Initialize(Application application)
    {
        lock (SyncRoot)
        {
            _current ??= new AppHost(
                new ServiceCollection()
                    .AddWsjtxWatcherServices(application)
                    .BuildServiceProvider());
        }
    }

    public ServiceProvider Services { get; }

    public T GetRequiredService<T>() where T : notnull
    {
        return Services.GetRequiredService<T>();
    }
}
