using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Views;
using WsjtxWatcher.Activities;
using WsjtxWatcher.Database;
using WsjtxWatcher.Dialogs;
using WsjtxWatcher.Utils.AppPackage;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnResetAllListener : Object, View.IOnClickListener
{
    private readonly Context _ctx;
    private ProgDialog _progDialog;

    public OnResetAllListener(Context ctx)
    {
        this._ctx = ctx;
    }

    public void OnClick(View? v)
    {
        // 显示等待动画
        _progDialog = new ProgDialog(_ctx);
        _progDialog.StartAni();

        // 执行后台任务
        Task.Run(() =>
        {
            try
            {
                // 执行数据库重置操作
                DatabaseHandler.GetInstance(null).ResetDatabase();

                // 删除 SharedPreferences
                var sharedPreferences = _ctx.GetSharedPreferences(_ctx.GetString(ResourceConstant.String.storage_key),
                    FileCreationMode.Private);
                sharedPreferences.Edit().Clear().Commit();
                DefaultSettings.SetDefault(v.Context);
            }
            catch (Exception ex)
            {
                // 处理异常（如果有必要）
                Log.Debug("RSTALL", ex.Message);
            }
            finally
            {
                // 确保在主线程中隐藏动画
                ((Activity)_ctx).RunOnUiThread(() => { _progDialog.StopAni(); });
                var intent = new Intent(Application.Context, typeof(MainActivity)); // 替换为你的启动活动
                intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
                var pendingIntent =
                    PendingIntent.GetActivity(Application.Context, 0, intent, PendingIntentFlags.CancelCurrent);
                var alarmManager = (AlarmManager)Application.Context.GetSystemService(Context.AlarmService);
                alarmManager.Set(AlarmType.Rtc, DateTime.Now.Millisecond + 600, pendingIntent); // 100 毫秒后重启
                Process.KillProcess(Process.MyPid()); // 结束当前进程
            }
        });
    }
}