using Android.App;
using Serilog;
using WsjtxWatcher.Core.Contracts;
using WsjtxWatcher.Infrastructure.Platform;

namespace WsjtxWatcher.App;

[Application]
public sealed class WsjtxWatcherApplication : Application
{
    public WsjtxWatcherApplication(IntPtr handle, Android.Runtime.JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    public override void OnCreate()
    {
        base.OnCreate();

        var appInfo = new AndroidAppInfoService(this);
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                appInfo.LogFilePath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}",
                rollOnFileSizeLimit: true,
                retainedFileCountLimit: 2,
                fileSizeLimitBytes: 5 * 1024 * 1024)
            .CreateLogger();

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Log.Fatal(args.ExceptionObject as Exception, "Unhandled exception");
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Log.Fatal(args.Exception, "Unobserved task exception");
            args.SetObserved();
        };

        AppHost.Initialize(this);
        AppHost.Current.GetRequiredService<AndroidAppLanguageManager>().ApplyCurrentLanguage();
        _ = AppHost.Current.GetRequiredService<IAppThemeService>();
    }
}
