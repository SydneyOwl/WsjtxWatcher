using Android.Content;
using Android.OS;
using Android.Views;
using WsjtxWatcher.Activities;
using WsjtxWatcher.Database;
using WsjtxWatcher.Dialogs;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnResetDBListener : Object,View.IOnClickListener
{
    private Context ctx;
    private ProgDialog progDialog;

    public OnResetDBListener(Context ctx)
    {
        this.ctx = ctx;
    }
    public void OnClick(View? v)
    { // 显示等待动画
        progDialog = new ProgDialog(ctx);
        progDialog.StartAni();

        // 执行后台任务
        Task.Run(() =>
        {
            try
            {
                DatabaseHandler.GetInstance(null).ResetDatabase();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                ((Activity)ctx).RunOnUiThread(() =>
                {
                    progDialog.StopAni();
                });
                // 重启
                Intent intent = new Intent(Application.Context, typeof(MainActivity)); // 替换为你的启动活动
                intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
                PendingIntent pendingIntent = PendingIntent.GetActivity(Application.Context, 0, intent, PendingIntentFlags.CancelCurrent);
                AlarmManager alarmManager = (AlarmManager)Application.Context.GetSystemService(Context.AlarmService);
                alarmManager.Set(AlarmType.Rtc, DateTime.Now.Millisecond + 600, pendingIntent); // 100 毫秒后重启
                Process.KillProcess(Process.MyPid()); // 结束当前进程
            }
        });
    }
}