using Android.App;
using Android.Content;
using WsjtxWatcher.Core.Contracts;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidLogFileService : ILogFileService
{
    private readonly Application _application;
    private readonly IAppInfoService _appInfoService;

    public AndroidLogFileService(Application application, IAppInfoService appInfoService)
    {
        _application = application;
        _appInfoService = appInfoService;
    }

    public void OpenLogFile()
    {
        if (!File.Exists(_appInfoService.LogFilePath))
        {
            return;
        }

        var intent = new Intent(_application, typeof(UI.Activities.LogViewerActivity));
        intent.AddFlags(ActivityFlags.NewTask);
        intent.PutExtra(UI.Activities.LogViewerActivity.LogFilePathExtra, _appInfoService.LogFilePath);
        _application.StartActivity(intent);
    }
}
