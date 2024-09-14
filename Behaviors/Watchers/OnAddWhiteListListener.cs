using Android.Content;
using Android.OS;
using Android.Provider;
using Android.Views;
using Object = Java.Lang.Object;
using Uri = Android.Net.Uri;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnAddWhiteListListener : Object, View.IOnClickListener
{
    public void OnClick(View? v)
    {
        var intent = new Intent();
        var packageName = v.Context.PackageName;
        var pm = (PowerManager)v.Context.GetSystemService(Context.PowerService);
        intent.SetAction(Settings.ActionRequestIgnoreBatteryOptimizations);
        intent.SetData(Uri.Parse("package:" + packageName));
        v.Context.StartActivity(intent);
    }
}