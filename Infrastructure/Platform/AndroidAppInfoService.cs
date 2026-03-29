using Android.App;
using WsjtxWatcher.Core.Contracts;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidAppInfoService : IAppInfoService
{
    public AndroidAppInfoService(Application application)
    {
        var packageInfo = application.PackageManager?.GetPackageInfo(application.PackageName!, 0);
        VersionName = packageInfo?.VersionName ?? "1.0";
        AppDataDirectory = application.FilesDir?.AbsolutePath ?? AppContext.BaseDirectory;
        LogFilePath = Path.Combine(AppDataDirectory, "wsjtxwatcher.log");
    }

    public string VersionName { get; }
    public string AppDataDirectory { get; }
    public string LogFilePath { get; }
}
