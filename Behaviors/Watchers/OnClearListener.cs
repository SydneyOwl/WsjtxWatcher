using Android.Views;
using WsjtxWatcher.Adapters;
using WsjtxWatcher.Utils.UdpServer;
using WsjtxWatcher.ViewModels;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnClearListener : Object, View.IOnClickListener
{
    public void OnClick(View? v)
    {
        MainViewModel.GetInstance().DecodedMsgList.Clear();
        UdpServer.GetInstance().SendClearMessage();
    }
}