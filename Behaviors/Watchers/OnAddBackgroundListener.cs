using Android.Views;
using WsjtxWatcher.Utils.Brand;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

using Object = Object;

public class OnAddBackgroundListener : Object, View.IOnClickListener
{
    public void OnClick(View? v)
    {
        try
        {
            if (ChkBrand.IsHuawei()) ChkBrand.GoHuaweiSetting(v.Context);
            else if (ChkBrand.IsMeizu()) ChkBrand.GoMeizuSetting(v.Context);
            else if (ChkBrand.IsSamsung()) ChkBrand.GoSamsungSetting(v.Context);
            else if (ChkBrand.IsSmartisan()) ChkBrand.GoSmartisanSetting(v.Context);
            else if (ChkBrand.IsXiaomi()) ChkBrand.GoXiaomiSetting(v.Context);
            else if (ChkBrand.IsLeTv()) ChkBrand.GoLetvSetting(v.Context);
            else if (ChkBrand.IsOppo()) ChkBrand.GoOppoSetting(v.Context);
            else if (ChkBrand.IsVivo()) ChkBrand.GoVivoSetting(v.Context);
        }
        catch
        {
            //ignored
        }
    }
}