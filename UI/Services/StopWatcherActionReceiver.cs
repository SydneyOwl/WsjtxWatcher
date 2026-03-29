using Android.App;
using Android.Content;

namespace WsjtxWatcher.UI.Services;

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class StopWatcherActionReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null)
        {
            return;
        }

        context.StopService(new Intent(context, typeof(MsgPushService)));
    }
}
