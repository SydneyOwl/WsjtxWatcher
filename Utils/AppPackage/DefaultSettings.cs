using _Microsoft.Android.Resource.Designer;
using Android.Content;
using WsjtxWatcher.Variables;

namespace WsjtxWatcher.Utils.AppPackage;

public class DefaultSettings
{
    public static void SetDefault(Context ctx)
    {
        var sharedPref =
            ctx.GetSharedPreferences(ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        var edit = sharedPref.Edit();
        edit.PutString("version", GlobalVariables.VersionTag);
        edit.PutStringSet("prefered_dxcc", new List<string>
        {
            // according to https://dxnews.com/dxcc-2017/  (DXCC Most Wanted 2024) TOP 10
            "257", // north korea
            "67", // SCARBOROUGH REEF
            "115", //SAN FELIX ISLANDS
            "71", // PRATAS ISLAND
            "229", //KURE ISLAND
            "225", //JOHNSTON ISLAND
            "17", //PETER 1 ISLAND
            "172", //KERGUELEN ISLAND
            "363", //AVES ISLAND
            "16" //BOUVET ISLAND
        });
        edit.Commit();
    }
}