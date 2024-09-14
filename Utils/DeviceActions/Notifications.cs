using _Microsoft.Android.Resource.Designer;
using Android.Content;
using DebounceThrottle;
using Microsoft.VisualBasic;

namespace WsjtxWatcher.Utils.DeviceActions;

public class Notifications
{
    private static Notifications _instance;
    private static readonly ThrottleDispatcher ThrottleDispatcher = new(TimeSpan.FromMilliseconds(10000));
    private readonly Context _ctx;
    private readonly NotificationManager _notificationManager;


    private Notifications(Context ctx)
    {
        this._ctx = ctx;
        _notificationManager = (NotificationManager)this._ctx.GetSystemService(Context.NotificationService);
        var channelMsgRecv = new NotificationChannel(ctx.GetString(ResourceConstant.String.notification_channel_id1),
            ctx.GetString(ResourceConstant.String.app_name),
            NotificationImportance.High);
        _notificationManager.CreateNotificationChannel(channelMsgRecv);
    }

    public void PopCommonNotification(string msg)
    {
        var notifications =
            new Notification.Builder(_ctx, _ctx.GetString(ResourceConstant.String.notification_channel_id1))
                .SetContentTitle(_ctx.GetString(ResourceConstant.String.user_ft8_msg_available))
                .SetContentText(msg)
                .SetWhen(DateAndTime.Now.Millisecond)
                .SetSmallIcon(ResourceConstant.Mipmap.appicon)
                .SetAutoCancel(true)
                .Build();
        ThrottleDispatcher.Throttle(() =>
        {
            _notificationManager.Notify(int.Parse(_ctx.GetString(ResourceConstant.String.notify_id1)), notifications);
        });
    }


    public static Notifications GetInstance(Context ctx)
    {
        if (_instance == null) _instance = new Notifications(ctx);

        return _instance;
    }
}