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