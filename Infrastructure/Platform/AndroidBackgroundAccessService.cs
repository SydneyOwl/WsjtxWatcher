using Android.App;
using Android.Content;
using Android.OS;
using Android.Provider;
using Serilog;
using Uri = Android.Net.Uri;
using WsjtxWatcher.Core.Contracts;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidBackgroundAccessService : IBackgroundAccessService
{
    private readonly Application _application;

    public AndroidBackgroundAccessService(Application application)
    {
        _application = application;
    }

    public bool IsIgnoringBatteryOptimizations()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.M)
        {
            return false;
        }

        var powerManager = (PowerManager?)_application.GetSystemService(Context.PowerService);
        return powerManager?.IsIgnoringBatteryOptimizations(_application.PackageName!) ?? false;
    }

    public void RequestIgnoreBatteryOptimizations()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.M || IsIgnoringBatteryOptimizations())
        {
            return;
        }

        var intent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
        intent.SetData(Uri.Parse("package:" + _application.PackageName));
        intent.AddFlags(ActivityFlags.NewTask);
        _application.StartActivity(intent);
    }

    public void OpenBackgroundSettings()
    {
        try
        {
            var brand = (Build.Brand ?? string.Empty).ToLowerInvariant();
            if (brand is "huawei" or "honor")
            {
                StartExplicitActivity("com.huawei.systemmanager", "com.huawei.systemmanager.startupmgr.ui.StartupNormalAppListActivity");
                return;
            }

            if (brand == "xiaomi")
            {
                StartExplicitActivity("com.miui.securitycenter", "com.miui.permcenter.autostart.AutoStartManagementActivity");
                return;
            }

            if (brand == "oppo")
            {
                if (!StartPackageActivity("com.coloros.phonemanager") &&
                    !StartPackageActivity("com.oppo.safe") &&
                    !StartPackageActivity("com.coloros.oppoguardelf"))
                {
                    StartPackageActivity("com.coloros.safecenter");
                }
                return;
            }

            if (brand == "vivo")
            {
                StartPackageActivity("com.iqoo.secure");
                return;
            }

            if (brand == "meizu")
            {
                StartPackageActivity("com.meizu.safe");
                return;
            }

            if (brand == "samsung")
            {
                if (!StartPackageActivity("com.samsung.android.sm_cn"))
                {
                    StartPackageActivity("com.samsung.android.sm");
                }
                return;
            }

            if (brand == "letv")
            {
                StartExplicitActivity("com.letv.android.letvsafe", "com.letv.android.letvsafe.AutobootManageActivity");
                return;
            }

            if (brand == "smartisan")
            {
                StartPackageActivity("com.smartisanos.security");
                return;
            }

            var settingsIntent = new Intent(Settings.ActionApplicationDetailsSettings);
            settingsIntent.SetData(Uri.Parse("package:" + _application.PackageName));
            settingsIntent.AddFlags(ActivityFlags.NewTask);
            _application.StartActivity(settingsIntent);
        }
        catch (Exception ex)
        {
            Log.Information(ex,"Error while OpenBackgroundSettings");
        }
    }

    private bool StartPackageActivity(string packageName)
    {
        try
        {
            var intent = _application.PackageManager?.GetLaunchIntentForPackage(packageName);
            if (intent is null)
            {
                return false;
            }

            intent.AddFlags(ActivityFlags.NewTask);
            _application.StartActivity(intent);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void StartExplicitActivity(string packageName, string activityName)
    {
        var intent = new Intent();
        intent.SetComponent(new ComponentName(packageName, activityName));
        intent.AddFlags(ActivityFlags.NewTask);
        _application.StartActivity(intent);
    }
}
