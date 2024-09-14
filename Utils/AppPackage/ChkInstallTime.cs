using _Microsoft.Android.Resource.Designer;
using Android.Content;
using WsjtxWatcher.Variables;

namespace WsjtxWatcher.Utils.AppPackage;

public class ChkInstallTime
{
    public static bool IsSameVersion(Context ctx)
    {
        var sharedPref =
            ctx.GetSharedPreferences(ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        return sharedPref.GetString("version", "null") == GlobalVariables.VersionTag;
    }

    public static void SetCurrentTag(Context ctx)
    {
        var sharedPref =
            ctx.GetSharedPreferences(ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        var edit = sharedPref.Edit();
        edit.PutString("version", GlobalVariables.VersionTag);
        edit.Apply();
    }
}