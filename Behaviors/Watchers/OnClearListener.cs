using Android.Views;
using WsjtxWatcher.Adapters;
using WsjtxWatcher.Utils.UdpServer;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnClearListener : Object, View.IOnClickListener
{
    private readonly CallItemAdapter _adapter;

    public OnClearListener(CallItemAdapter adapter)
    {
        this._adapter = adapter;
    }

    public void OnClick(View? v)
    {
        _adapter.Clear();
        UdpServer.GetInstance().SendClearMessage();
    }
}