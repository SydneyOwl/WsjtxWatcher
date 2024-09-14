using Android.Content;
using Android.Views;
using WsjtxWatcher.Activities;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnJumpDxccListener : Object, View.IOnClickListener
{
    private readonly Context _ctx;

    public OnJumpDxccListener(Context ctx)
    {
        this._ctx = ctx;
    }

    public void OnClick(View? v)
    {
        var intent = new Intent(_ctx, typeof(SetDxccActivity));
        _ctx.StartActivity(intent);
    }
}