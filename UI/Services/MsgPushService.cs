using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using WsjtxWatcher.App;
using WsjtxWatcher.Core.Services;

namespace WsjtxWatcher.UI.Services;

[Service(
    Name = "com.sydneyowl.WsjtxWatcher.Services.MsgPushService",
    Exported = false)]
public sealed class MsgPushService : Service
{
    private CancellationTokenSource? _serviceLifetimeCts;

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override void OnCreate()
    {
        base.OnCreate();
        _serviceLifetimeCts = new CancellationTokenSource();
        StartForegroundInternal();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        _ = StartWatcherAsync(startId, _serviceLifetimeCts?.Token ?? CancellationToken.None);
        return StartCommandResult.Sticky;
    }

    public override void OnDestroy()
    {
        _serviceLifetimeCts?.Cancel();
        _ = StopWatcherAsync();
        _serviceLifetimeCts?.Dispose();
        _serviceLifetimeCts = null;

        StopForeground(StopForegroundFlags.Remove);
        base.OnDestroy();
    }

    private async Task StartWatcherAsync(int startId, CancellationToken cancellationToken)
    {
        try
        {
            await AppHost.Current.GetRequiredService<WatcherController>().StartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (System.OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            Serilog.Log.Error(exception, "Failed to start WSJT-X watcher service.");
            StopForeground(StopForegroundFlags.Remove);
            StopSelfResult(startId);
        }
    }

    private static async Task StopWatcherAsync()
    {
        try
        {
            await AppHost.Current.GetRequiredService<WatcherController>().StopAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Serilog.Log.Warning(exception, "Failed to stop WSJT-X watcher service cleanly.");
        }
    }

    private void StartForegroundInternal()
    {
        var notificationManager = (NotificationManager)GetSystemService(NotificationService)!;
        var channel = new NotificationChannel(
            GetString(Resource.String.notification_channel_id2),
            GetString(Resource.String.app_name),
            NotificationImportance.Default);
        notificationManager.CreateNotificationChannel(channel);

        var launchIntent = new Intent(this, typeof(Activities.MainActivity));
        launchIntent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        var pendingIntent = PendingIntent.GetActivity(
            this,
            0,
            launchIntent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var notification = new Notification.Builder(this, GetString(Resource.String.notification_channel_id2))
            .SetContentTitle(GetString(Resource.String.service_running))
            .SetContentText(GetString(Resource.String.listening_msg))
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentIntent(pendingIntent)
            .SetOngoing(true)
            .Build();

        if (OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            StartForeground(int.Parse(GetString(Resource.String.notify_id2)), notification, ForegroundService.TypeConnectedDevice);
        }
        else
        {
            StartForeground(int.Parse(GetString(Resource.String.notify_id2)), notification);
        }
    }
}
