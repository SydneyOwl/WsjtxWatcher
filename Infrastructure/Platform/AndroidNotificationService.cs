using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Provider;
using WsjtxWatcher.Core.Contracts;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidNotificationService : INotificationService
{
    private readonly Application _application;
    private readonly NotificationManager _notificationManager;
    private DateTimeOffset _lastNotificationAt = DateTimeOffset.MinValue;

    public AndroidNotificationService(Application application)
    {
        _application = application;
        _notificationManager = (NotificationManager)application.GetSystemService(Context.NotificationService)!;
        var channel = new NotificationChannel(
            application.GetString(Resource.String.notification_channel_id1),
            application.GetString(Resource.String.app_name),
            NotificationImportance.High);
        _notificationManager.CreateNotificationChannel(channel);
    }

    public bool AreNotificationsEnabled()
    {
        if (!_notificationManager.AreNotificationsEnabled())
        {
            return false;
        }

        return !OperatingSystem.IsAndroidVersionAtLeast(33)
               || _application.CheckSelfPermission(Android.Manifest.Permission.PostNotifications) == Permission.Granted;
    }

    public void OpenNotificationSettings()
    {
        var settingsIntent = new Intent(Settings.ActionAppNotificationSettings);
        settingsIntent.PutExtra(Settings.ExtraAppPackage, _application.PackageName);
        settingsIntent.AddFlags(ActivityFlags.NewTask);

        try
        {
            _application.StartActivity(settingsIntent);
        }
        catch
        {
            var appDetailsIntent = new Intent(Settings.ActionApplicationDetailsSettings);
            appDetailsIntent.SetData(Android.Net.Uri.Parse("package:" + _application.PackageName));
            appDetailsIntent.AddFlags(ActivityFlags.NewTask);
            _application.StartActivity(appDetailsIntent);
        }
    }

    public Task ShowMessageAlertAsync(string message, CancellationToken cancellationToken = default)
    {
        if (!AreNotificationsEnabled())
        {
            return Task.CompletedTask;
        }

        if (DateTimeOffset.UtcNow - _lastNotificationAt < TimeSpan.FromSeconds(10))
        {
            return Task.CompletedTask;
        }

        _lastNotificationAt = DateTimeOffset.UtcNow;
        var intent = new Intent(_application, typeof(UI.Activities.MainActivity));
        intent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        intent.PutExtra(UI.Activities.MainActivity.ScrollToBottomFromNotificationExtra, true);
        var pendingIntent = PendingIntent.GetActivity(
            _application,
            0,
            intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var notification = new Notification.Builder(_application, _application.GetString(Resource.String.notification_channel_id1))
            .SetContentTitle(_application.GetString(Resource.String.user_ft8_msg_available))
            .SetContentText(message)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetContentIntent(pendingIntent)
            .SetAutoCancel(true)
            .Build();

        _notificationManager.Notify(int.Parse(_application.GetString(Resource.String.notify_id1)), notification);
        return Task.CompletedTask;
    }
}
