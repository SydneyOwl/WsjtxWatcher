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
        finally
        {
                AlertDialog.Builder builder = new AlertDialog.Builder(ctx);
                builder.SetTitle("注意");
                builder.SetMessage(@"如果未能弹出相应设置窗口，请您参考以下步骤更改白名单设置（以HarmonyOS为例，其他手机型号大同小异，可作参考）：
1、在手机自带的设置里，打开【应用与服务】，选定【应用管理】。
2、在列表里翻找出自己想要保持在后台运行的APP，或在搜索框里搜索Wsjtx，进入应用的“应用信息”界面。
3、进入【耗电详情】，点击【启动管理】，关闭【自动管理】，并且打开手动管理的【允许后台活动】选项。");
                builder.SetNeutralButton("确定",((sender, args) => {}));
                AlertDialog alertDialog = builder.Create();//这个方法可以返回一个alertDialog对象
                alertDialog.Show();
        }
    }
}