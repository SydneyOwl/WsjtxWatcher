using _Microsoft.Android.Resource.Designer;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using WsjtxWatcher.Utils.Network;
using WsjtxWatcher.Utils.UdpServer;
using WsjtxWatcher.Variables;
using WsjtxWatcher.ViewModels;
using Resource = Android.Resource;

[Service]
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
            var conf = MainViewModel.GetInstance().udpConf;
            UdpServer.getInstance().startServer(conf);
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
        
        NotificationChannel channel_serviceStart = new NotificationChannel(GetString(ResourceConstant.String.notification_channel_id2),GetString(ResourceConstant.String.app_name),
            NotificationImportance.Default);
        notificationManager.CreateNotificationChannel(channel_serviceStart);
        
        var stopIntent = new Intent(this, GetType());
        stopIntent.SetAction("STOP_SERVICE");
        var pendingIntent = PendingIntent.GetService(this, 0, stopIntent, PendingIntentFlags.UpdateCurrent);

        var notification = new Notification.Builder(this, GetString(ResourceConstant.String.notification_channel_id2))
            .SetContentTitle(GetString(ResourceConstant.String.service_running))
            .SetContentText(GetString(ResourceConstant.String.listening_msg))
            .SetSmallIcon(ResourceConstant.Mipmap.appicon)
            .SetContentIntent(pendingIntent)
            .Build();

        StartForeground(int.Parse(GetString(ResourceConstant.String.notify_id2)), notification);
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        Log.Info(Tag, "OnDestroy: ");
        UdpServer.getInstance().stopServer();
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