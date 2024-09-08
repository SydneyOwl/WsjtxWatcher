using Android.Views;
using WsjtxWatcher.Database;
using WsjtxWatcher.Utils.UdpServer;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnHaltTxListener : Object,View.IOnClickListener
{
    public void OnClick(View? v)
    {
        UdpServer.getInstance().SendHaltTxMessage();
    }
}