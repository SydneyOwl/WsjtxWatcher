using Android.Views;
using WsjtxWatcher.Utils.Brand;

namespace WsjtxWatcher.Behaviors.Watchers;
using Object = Java.Lang.Object;
public class OnAddBackgroundListener:Object,View.IOnClickListener
{
    public void OnClick(View? v)
    {
        try
        {
            if (ChkBrand.isHuawei()) ChkBrand.goHuaweiSetting(v.Context);
            else if (ChkBrand.isMeizu()) ChkBrand.goMeizuSetting(v.Context);
            else if (ChkBrand.isSamsung()) ChkBrand.goSamsungSetting(v.Context);
            else if (ChkBrand.isSmartisan()) ChkBrand.goSmartisanSetting(v.Context);
            else if (ChkBrand.isXiaomi()) ChkBrand.goXiaomiSetting(v.Context);
            else if (ChkBrand.isLeTV()) ChkBrand.goLetvSetting(v.Context);
            else if (ChkBrand.isOPPO()) ChkBrand.goOPPOSetting(v.Context);
            else if (ChkBrand.isVIVO()) ChkBrand.goVIVOSetting(v.Context);
        }
        catch
        {
            //ignored
        }
    }
}