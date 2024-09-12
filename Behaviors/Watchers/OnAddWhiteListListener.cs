using Android.Content;
using Android.Health.Connect.DataTypes;
using Android.OS;
using Android.Views;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnAddWhiteListListener:Object,View.IOnClickListener
{
    public void OnClick(View? v)
    {
        Intent intent = new Intent();
        string packageName = v.Context.PackageName;
        PowerManager pm = (PowerManager)v.Context.GetSystemService(Service.PowerService);
        intent.SetAction(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
        intent.SetData(Android.Net.Uri.Parse("package:" + packageName));
        v.Context.StartActivity(intent);
    }
}