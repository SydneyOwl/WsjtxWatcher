using Android.Content;
using Android.Views;
using WsjtxWatcher.Database;

using Object = Java.Lang.Object;
namespace WsjtxWatcher.Behaviors.Watchers;

public class OnResetAllListener: Object,View.IOnClickListener
{
    public void OnClick(View? v)
    {
        DatabaseHandler.GetInstance(null).resetDatabase();
        v.Context.DeleteSharedPreferences(v.Context.GetString(Resource.String.storage_key));
    }
}