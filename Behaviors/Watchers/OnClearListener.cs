using Android.Views;
using WsjtxWatcher.Activities;
using WsjtxWatcher.Adapters;
using WsjtxWatcher.Database;
using WsjtxWatcher.Utils.UdpServer;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnClearListener : Object,View.IOnClickListener
{
    private CallItemAdapter adapter;
    public OnClearListener(CallItemAdapter adapter)
    {
        this.adapter = adapter;
    }
    public void OnClick(View? v)
    {
        adapter.Clear();
        UdpServer.getInstance().SendClearMessage();
    }
}