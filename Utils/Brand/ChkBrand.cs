using Android.Content;
using Android.OS;

namespace WsjtxWatcher.Utils.Brand;

public class ChkBrand
{
    private static void ShowActivity(Context ctx, string packageName)
    {
        var intent = ctx.PackageManager.GetLaunchIntentForPackage(packageName);
        ctx.StartActivity(intent);
    }

    private static void ShowActivity(Context ctx, string packageName, string activityDir)
    {
        var intent = new Intent();
        intent.SetComponent(new ComponentName(packageName, activityDir));
        intent.AddFlags(ActivityFlags.NewTask);
        ctx.StartActivity(intent);
    }

    public static bool IsHuawei()
    {
        if (Build.Brand == null) return false;
        return Build.Brand.ToLower().Equals("huawei") || Build.Brand.ToLower().Equals("honor");
    }

    public static void GoHuaweiSetting(Context ctx)
    {
        try
        {
            ShowActivity(ctx, "com.huawei.systemmanager",
                "com.huawei.systemmanager.startupmgr.ui.StartupNormalAppListActivity");
        }
        catch (Exception e)
        {
            ShowActivity(ctx, "com.huawei.systemmanager",
                "com.huawei.systemmanager.optimize.bootstart.BootStartActivity");
        }
        finally
        {
            AlertDialog.Builder builder = new AlertDialog.Builder(ctx);
            builder.SetTitle("注意");
            builder.SetMessage(@"如果未能弹出相应设置窗口，请您参考以下步骤更改白名单设置（以HarmonyOS为例，其他手机型号大同小异，可作参考）：
1、在手机自带的设置里，打开【应用与服务】，选定【应用管理】。
2、在列表里翻找出自己想要保持在后台运行的APP，或在搜索框里搜索Wsjtx，进入应用的“应用信息”界面。
3、进入【耗电详情】，点击【启动管理】，关闭【自动管理】，并且打开手动管理的【允许后台活动】选项。");
            builder.SetNeutralButton("确定",((sender, args) => {}));
            AlertDialog alertDialog = builder.Create();//这个方法可以返回一个alertDialog对象
            alertDialog.Show();
        }
    }

    public static bool IsXiaomi()
    {
        return Build.Brand != null && Build.Brand.ToLower().Equals("xiaomi");
    }

    public static void GoXiaomiSetting(Context ctx)
    {
        ShowActivity(ctx, "com.miui.securitycenter",
            "com.miui.permcenter.autostart.AutoStartManagementActivity");
    }

    public static bool IsOppo()
    {
        return Build.Brand != null && Build.Brand.ToLower().Equals("oppo");
    }

    public static void GoOppoSetting(Context ctx)
    {
        try
        {
            ShowActivity(ctx, "com.coloros.phonemanager");
        }
        catch (Exception e1)
        {
            try
            {
                ShowActivity(ctx, "com.oppo.safe");
            }
            catch (Exception e2)
            {
                try
                {
                    ShowActivity(ctx, "com.coloros.oppoguardelf");
                }
                catch (Exception e3)
                {
                    ShowActivity(ctx, "com.coloros.safecenter");
                }
            }
        }
    }

    public static bool IsVivo()
    {
        return Build.Brand != null && Build.Brand.ToLower().Equals("vivo");
    }

    public static void GoVivoSetting(Context ctx)
    {
        ShowActivity(ctx, "com.iqoo.secure");
    }

    public static bool IsMeizu()
    {
        return Build.Brand != null && Build.Brand.ToLower().Equals("meizu");
    }

    public static void GoMeizuSetting(Context ctx)
    {
        ShowActivity(ctx, "com.meizu.safe");
    }

    public static bool IsSamsung()
    {
        return Build.Brand != null && Build.Brand.ToLower().Equals("samsung");
    }

    public static void GoSamsungSetting(Context ctx)
    {
        try
        {
            ShowActivity(ctx, "com.samsung.android.sm_cn");
        }
        catch (Exception e)
        {
            ShowActivity(ctx, "com.samsung.android.sm");
        }
    }

    public static bool IsLeTv()
    {
        return Build.Brand != null && Build.Brand.ToLower().Equals("letv");
    }

    public static void GoLetvSetting(Context ctx)
    {
        ShowActivity(ctx, "com.letv.android.letvsafe",
            "com.letv.android.letvsafe.AutobootManageActivity");
    }

    public static bool IsSmartisan()
    {
        return Build.Brand != null && Build.Brand.ToLower().Equals("smartisan");
    }

    public static void GoSmartisanSetting(Context ctx)
    {
        ShowActivity(ctx, "com.smartisanos.security");
    }
}