using Android.App;
using Android.Content;
using WsjtxWatcher.App;
using WsjtxWatcher.Infrastructure.Platform;

namespace WsjtxWatcher.UI.Activities;

public abstract class LocalizedActivity : Activity
{
    protected override void AttachBaseContext(Context? @base)
    {
        if (@base is null || !AppHost.IsInitialized)
        {
            base.AttachBaseContext(@base);
            return;
        }

        var languageManager = AppHost.Current.GetRequiredService<AndroidAppLanguageManager>();
        base.AttachBaseContext(languageManager.CreateLocalizedContext(@base));
    }
}
