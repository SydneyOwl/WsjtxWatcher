using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Util;
using WsjtxWatcher.Utils.UdpServer;
using WsjtxWatcher.ViewModels;

[Service(Name = "com.sydneyowl.WsjtxWatcher.Services.MsgPushService")]
public class MsgPushService : Service
{
    private const string Tag = "BaseService";

    public override IBinder OnBind(Intent intent)
    {
        throw new NotImplementedException("OnBind not implemented");
    }

    public override StartCommandResult OnStartCommand(Intent intent, StartCommandFlags flags, int startId)
    {
        if (intent?.Action == "STOP_SERVICE")
        {
            MainViewModel.GetInstance().IsMsgServiceRunning = false;
            StopSelf();
        }
        else
        {
            Log.Info("MyService", "Service Started");
            var conf = MainViewModel.GetInstance().UdpConf;
            UdpServer.GetInstance().StartServer(conf);
            MainViewModel.GetInstance().IsMsgServiceRunning = true;
        }

        return StartCommandResult.Sticky;
    }


    public override void OnCreate()
    {
        base.OnCreate();
        Log.Info(Tag, "OnCreate: ");
        StartForegroundService();
    }

    private void StartForegroundService()
    {
        var notificationManager = (NotificationManager)GetSystemService(NotificationService);

        var channelServiceStart = new NotificationChannel(GetString(ResourceConstant.String.notification_channel_id2),
            GetString(ResourceConstant.String.app_name),
            NotificationImportance.Default);
        notificationManager.CreateNotificationChannel(channelServiceStart);

        var stopIntent = new Intent(this, GetType());
        stopIntent.SetAction("STOP_SERVICE");
        PendingIntent pendingIntent;
        if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.S)
        {
            pendingIntent = PendingIntent.GetService(this, 0, stopIntent, PendingIntentFlags.Immutable);
        }
        else
        {
            pendingIntent = PendingIntent.GetService(this, 0, stopIntent, PendingIntentFlags.OneShot);
        }

        var notification = new Notification.Builder(this, GetString(ResourceConstant.String.notification_channel_id2))
            .SetContentTitle(GetString(ResourceConstant.String.service_running))
            .SetContentText(GetString(ResourceConstant.String.listening_msg))
            .SetSmallIcon(ResourceConstant.Mipmap.appicon)
            .SetContentIntent(pendingIntent)
            .Build();

        if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            StartForeground(int.Parse(GetString(ResourceConstant.String.notify_id2)), notification,ForegroundService.TypeConnectedDevice);
        }
        else
        {
            StartForeground(int.Parse(GetString(ResourceConstant.String.notify_id2)), notification);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Log.Info(Tag, "OnDestroy: ");
        UdpServer.GetInstance().StopServer();
    }

    public override void OnRebind(Intent intent)
    {
        Log.Info(Tag, "OnRebind: ");
        base.OnRebind(intent);
    }

    public override bool OnUnbind(Intent intent)
    {
        Log.Info(Tag, "OnUnbind: ");
        return base.OnUnbind(intent);
    }
}