using Android.Content;
using Android.Views;
using AndroidX.Core.Content;
using File = Java.IO.File;
using Uri = Android.Net.Uri;

namespace WsjtxWatcher.Behaviors.Watchers;
using Object = Java.Lang.Object;

public class OnOpenLogListener : Object, View.IOnClickListener
{
    private Context ctx;

    public OnOpenLogListener(Context ctx)
    {
        this.ctx = ctx;
    }

    public void OnClick(View? v)
    {
        var filePath = new File(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WsjtxWatcher_log.txt"));
        if (filePath.Exists())
        {
            var uri = FileProvider.GetUriForFile(Android.App.Application.Context, $"{Android.App.Application.Context.PackageName}.fileprovider", filePath);
            var intent = new Intent(Intent.ActionView);
            intent.AddCategory("android.intent.category.DEFAULT");
            intent.AddFlags(ActivityFlags.NewTask);
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            intent.SetDataAndType(uri, "text/plain");
           try
            {
                ctx.StartActivity(intent);
            }
            catch (Exception e)
            {
                Serilog.Log.Information(e.Message);
                Toast.MakeText(ctx, "没有可以打开此文件的应用", ToastLength.Short)?.Show();
            }
        }
    }
}