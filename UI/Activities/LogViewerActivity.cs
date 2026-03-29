using Android.App;
using Android.OS;
using Android.Widget;

namespace WsjtxWatcher.UI.Activities;

[Activity(Label = "@string/open_log", Exported = false)]
public sealed class LogViewerActivity : LocalizedActivity
{
    public const string LogFilePathExtra = "log_file_path";

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(Resource.Layout.activity_log_viewer);

        var contentView = FindViewById<TextView>(Resource.Id.log_content)!;
        var filePath = Intent?.GetStringExtra(LogFilePathExtra);

        contentView.Text = !string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath)
            ? File.ReadAllText(filePath)
            : string.Empty;
    }
}
