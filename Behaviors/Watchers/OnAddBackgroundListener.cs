using Android.Content;
using Android.Views;
using WsjtxWatcher.Utils.Brand;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

using Object = Object;

public class OnAddBackgroundListener : Object, View.IOnClickListener
{
    private Context ctx;
    public OnAddBackgroundListener(Context ctx)
    {
        this.ctx = ctx;
    }
    public void OnClick(View? v)
    {
        try
        {
            if (ChkBrand.IsHuawei()) ChkBrand.GoHuaweiSetting(ctx);
            else if (ChkBrand.IsMeizu()) ChkBrand.GoMeizuSetting(ctx);
            else if (ChkBrand.IsSamsung()) ChkBrand.GoSamsungSetting(ctx);
            else if (ChkBrand.IsSmartisan()) ChkBrand.GoSmartisanSetting(ctx);
            else if (ChkBrand.IsXiaomi()) ChkBrand.GoXiaomiSetting(ctx);
            else if (ChkBrand.IsLeTv()) ChkBrand.GoLetvSetting(ctx);
            else if (ChkBrand.IsOppo()) ChkBrand.GoOppoSetting(ctx);
            else if (ChkBrand.IsVivo()) ChkBrand.GoVivoSetting(ctx);
        }
        catch
        {
            //ignored
        }
    }
}