using Android.Content;
using Android.Views;
using WsjtxWatcher.Activities;
using Object = Java.Lang.Object;
namespace WsjtxWatcher.Behaviors.Watchers;

public class OnJumpDxccListener:Object,View.IOnClickListener
{

    private Context ctx;
    public OnJumpDxccListener(Context ctx)
    {
        this.ctx = ctx;
    }
    public void OnClick(View? v)
    {
        var intent = new Intent(ctx, typeof(SetDxccActivity));
        ctx.StartActivity(intent);
    }
}