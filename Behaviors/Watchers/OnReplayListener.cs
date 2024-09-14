using Android.Views;
using WsjtxWatcher.Utils.UdpServer;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnReplayListener : Object, View.IOnClickListener
{
    public void OnClick(View? v)
    {
        UdpServer.GetInstance().SendReplayMessage();
    }
}