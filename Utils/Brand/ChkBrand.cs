using Android.Content;
using Android.OS;

namespace WsjtxWatcher.Utils.Brand;

public class ChkBrand
{
    private static void showActivity(Context ctx,String packageName) {
        Intent intent = ctx.PackageManager.GetLaunchIntentForPackage(packageName);
        ctx.StartActivity(intent);
    }
 
    private static void showActivity(Context ctx,String packageName, String activityDir) {
        Intent intent = new Intent();
        intent.SetComponent(new ComponentName(packageName, activityDir));
        intent.AddFlags(ActivityFlags.NewTask);
        ctx.StartActivity(intent);
    }
    
    public static bool isHuawei()
    {
        if (Build.Brand == null) {
            return false;
        }
        return Build.Brand.ToLower().Equals("huawei") || Build.Brand.ToLower().Equals("honor");
    }
    public static void goHuaweiSetting(Context ctx) {
        try {
            showActivity(ctx,"com.huawei.systemmanager",
                "com.huawei.systemmanager.startupmgr.ui.StartupNormalAppListActivity");
        } catch (Exception e) {
            showActivity(ctx,"com.huawei.systemmanager",
                "com.huawei.systemmanager.optimize.bootstart.BootStartActivity");
        }
    }
    public static bool isXiaomi() {
        return Build.Brand != null && Build.Brand.ToLower().Equals("xiaomi");
    }
    public static void goXiaomiSetting(Context ctx) {
        showActivity(ctx,"com.miui.securitycenter",
            "com.miui.permcenter.autostart.AutoStartManagementActivity");
    }
    public static bool isOPPO() {
        return Build.Brand != null && Build.Brand.ToLower().Equals("oppo");
    }
    public static void goOPPOSetting(Context ctx) {
        try {
            showActivity(ctx,"com.coloros.phonemanager");
        } catch (Exception e1) {
            try {
                showActivity(ctx,"com.oppo.safe");
            } catch (Exception e2) {
                try {
                    showActivity(ctx,"com.coloros.oppoguardelf");
                } catch (Exception e3) {
                    showActivity(ctx,"com.coloros.safecenter");
                }
            }
        }
    }
    public static bool isVIVO() {
        return Build.Brand != null && Build.Brand.ToLower().Equals("vivo");
    }
    public static void goVIVOSetting(Context ctx) {
        showActivity(ctx,"com.iqoo.secure");
    }
    public static bool isMeizu() {
        return Build.Brand != null && Build.Brand.ToLower().Equals("meizu");
    }
    public static void goMeizuSetting(Context ctx) {
        showActivity(ctx,"com.meizu.safe");
    }
    public static bool isSamsung() {
        return Build.Brand != null && Build.Brand.ToLower().Equals("samsung");
    }
    public static void goSamsungSetting(Context ctx) {
        try {
            showActivity(ctx,"com.samsung.android.sm_cn");
        } catch (Exception e) {
            showActivity(ctx,"com.samsung.android.sm");
        }
    }
    public static bool isLeTV() {
        return Build.Brand != null && Build.Brand.ToLower().Equals("letv");
    }
    public static void goLetvSetting(Context ctx) {
        showActivity(ctx,"com.letv.android.letvsafe",
            "com.letv.android.letvsafe.AutobootManageActivity");
    }
    public static bool isSmartisan() {
        return Build.Brand != null && Build.Brand.ToLower().Equals("smartisan");
    }
    
    public static void goSmartisanSetting(Context ctx) {
        showActivity(ctx,"com.smartisanos.security");
    }
}