using Android.Content;
using Android.Views;
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
                // 执行数据库重置操作
                DatabaseHandler.GetInstance(null).ResetDatabase();
            }
            catch (Exception ex)
            {
                // 处理异常（如果有必要）
                Console.WriteLine(ex.Message);
            }
            finally
            {
                // 确保在主线程中隐藏动画
                ((Activity)ctx).RunOnUiThread(() =>
                {
                    progDialog.StopAni();
                });
            }
        });
    }
}