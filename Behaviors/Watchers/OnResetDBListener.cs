using Android.Views;
using WsjtxWatcher.Database;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnResetDBListener : Object,View.IOnClickListener
{
    public void OnClick(View? v)
    {
        DatabaseHandler.GetInstance(null).resetDatabase();
    }
}