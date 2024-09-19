using Android.Content;
using Android.OS;
using Android.Views;
using WsjtxWatcher.Activities;
using WsjtxWatcher.Database;
using WsjtxWatcher.Dialogs;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnResetDbListener : Object, View.IOnClickListener
{
    private readonly Context _ctx;
    private ProgDialog _progDialog;

    public OnResetDbListener(Context ctx)
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
                DatabaseHandler.GetInstance(null).ResetDatabase();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"Failed to reset database: {ex.Message}");
            }
            finally
            {
                ((Activity)_ctx).RunOnUiThread(() => { _progDialog.StopAni(); });
                // 重启
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