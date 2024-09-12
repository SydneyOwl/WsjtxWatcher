using Android.Content;
using DebounceThrottle;
using Microsoft.VisualBasic;

namespace WsjtxWatcher.Utils.DeviceActions;

public class Notifications
{
    private Context ctx;
    private NotificationManager notificationManager;
    private static Notifications _instance;
    private static ThrottleDispatcher throttleDispatcher = new (TimeSpan.FromMilliseconds(10000));


    private Notifications(Context ctx)
    {
        this.ctx = ctx;
        notificationManager  = (NotificationManager)this.ctx.GetSystemService(Context.NotificationService);
        NotificationChannel channel = new NotificationChannel(this.ctx.GetString(Resource.String.app_name),ctx.GetString(Resource.String.app_name),
            NotificationImportance.High);
        notificationManager.CreateNotificationChannel(channel);
    }

    public void PopNotification(string msg)
    {
        Notification notifications = new Notification.Builder(ctx,ctx.GetString(Resource.String.app_name))
            .SetContentTitle(ctx.GetString(Resource.String.user_ft8_msg_available))
            .SetContentText(msg)
            .SetWhen(DateAndTime.Now.Millisecond)
            .SetSmallIcon(Resource.Mipmap.appicon)
            .SetAutoCancel(true)
            .Build();
        throttleDispatcher.Throttle(() =>
        {
            notificationManager.Notify(9342, notifications);
        });
    }

    public static Notifications getInstance(Context ctx)
    {
        if (_instance == null)
        {
            _instance = new Notifications(ctx);
        }

        return _instance;
    }
}