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
                if (TryStartExplicitActivity("com.huawei.systemmanager", "com.huawei.systemmanager.startupmgr.ui.StartupNormalAppListActivity") ||
                    StartPackageActivity("com.huawei.systemmanager"))
                {
                    return;
                }
            }
            else if (brand == "xiaomi")
            {
                if (TryStartExplicitActivity("com.miui.securitycenter", "com.miui.permcenter.autostart.AutoStartManagementActivity"))
                {
                    return;
                }
            }
            else if (brand == "oppo")
            {
                if (!StartPackageActivity("com.coloros.phonemanager") &&
                    !StartPackageActivity("com.oppo.safe") &&
                    !StartPackageActivity("com.coloros.oppoguardelf"))
                {
                    StartPackageActivity("com.coloros.safecenter");
                }
                return;
            }
            else if (brand == "vivo")
            {
                StartPackageActivity("com.iqoo.secure");
                return;
            }
            else if (brand == "meizu")
            {
                StartPackageActivity("com.meizu.safe");
                return;
            }
            else if (brand == "samsung")
            {
                if (!StartPackageActivity("com.samsung.android.sm_cn"))
                {
                    StartPackageActivity("com.samsung.android.sm");
                }
                return;
            }
            else if (brand == "letv")
            {
                if (TryStartExplicitActivity("com.letv.android.letvsafe", "com.letv.android.letvsafe.AutobootManageActivity"))
                {
                    return;
                }
            }
            else if (brand == "smartisan")
            {
                StartPackageActivity("com.smartisanos.security");
                return;
            }

            OpenAppDetailsSettings();
        }
        catch (Exception ex)
        {
            Log.Information(ex, "Error while OpenBackgroundSettings");
            OpenAppDetailsSettings();
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

    private bool TryStartExplicitActivity(string packageName, string activityName)
    {
        try
        {
            var intent = new Intent();
            intent.SetComponent(new ComponentName(packageName, activityName));
            intent.AddFlags(ActivityFlags.NewTask);
            _application.StartActivity(intent);
            return true;
        }
        catch (Exception ex)
        {
            Log.Information(ex, "Failed to open background settings component {PackageName}/{ActivityName}.", packageName, activityName);
            return false;
        }
    }

    private void OpenAppDetailsSettings()
    {
        var settingsIntent = new Intent(Settings.ActionApplicationDetailsSettings);
        settingsIntent.SetData(Uri.Parse("package:" + _application.PackageName));
        settingsIntent.AddFlags(ActivityFlags.NewTask);
        _application.StartActivity(settingsIntent);
    }
}
