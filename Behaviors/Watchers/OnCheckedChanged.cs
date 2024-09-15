using _Microsoft.Android.Resource.Designer;
using Android;
using Android.Content;
using Android.Content.PM;
using WsjtxWatcher.Variables;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnCheckedChanged
{
    private readonly Context _ctx;

    public OnCheckedChanged(Context ctx)
    {
        this._ctx = ctx;
    }

    public void SendNotificationCheckboxChanged(object? o, CompoundButton.CheckedChangeEventArgs args)
    {
        SettingsVariables.SendNotificationOnCall = args.IsChecked;
        // save conf
        var sharedPref =
            _ctx.GetSharedPreferences(_ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        var edit = sharedPref.Edit();
        edit.PutBoolean("send_notification_on_call", args.IsChecked);
        edit.Apply();

        if (_ctx.CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted)
        {
            ((Activity)_ctx).RequestPermissions(new[] {Manifest.Permission.PostNotifications }, 645);
        }
        //     Toast.MakeText(this, ResourceConstant.String.denied_vibrate, ToastLength.Short);
    }

    public void VibrateCheckboxChanged(object? o, CompoundButton.CheckedChangeEventArgs args)
    {
        SettingsVariables.VibrateOnCall = args.IsChecked;
        // save conf
        var sharedPref =
            _ctx.GetSharedPreferences(_ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        var edit = sharedPref.Edit();
        edit.PutBoolean("vibrate_on_call", args.IsChecked);
        edit.Apply();
        if (_ctx.CheckSelfPermission(Manifest.Permission.Vibrate) != Permission.Granted)
        {
            ((Activity)_ctx).RequestPermissions(new[] {Manifest.Permission.Vibrate }, 645);
        }
    }

    public void SendNotificationAllCheckboxChanged(object? o, CompoundButton.CheckedChangeEventArgs args)
    {
        SettingsVariables.SendNotificationOnAll = args.IsChecked;
        // save conf
        var sharedPref =
            _ctx.GetSharedPreferences(_ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        var edit = sharedPref.Edit();
        edit.PutBoolean("send_notification_on_all", args.IsChecked);
        edit.Apply();
        if (_ctx.CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted)
        {
            ((Activity)_ctx).RequestPermissions(new[] {Manifest.Permission.PostNotifications }, 645);
        }
    }

    public void VibrateAllCheckboxChanged(object? o, CompoundButton.CheckedChangeEventArgs args)
    {
        SettingsVariables.VibrateOnAll = args.IsChecked;
        // save conf
        var sharedPref =
            _ctx.GetSharedPreferences(_ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        var edit = sharedPref.Edit();
        edit.PutBoolean("vibrate_on_all", args.IsChecked);
        edit.Apply();
        if (_ctx.CheckSelfPermission(Manifest.Permission.Vibrate) != Permission.Granted)
        {
            ((Activity)_ctx).RequestPermissions(new[] {Manifest.Permission.Vibrate }, 645);
        }
    }

    public void SendNotificationDxccCheckboxChanged(object? o, CompoundButton.CheckedChangeEventArgs args)
    {
        SettingsVariables.SendNotificationOnDxcc = args.IsChecked;
        // save conf
        var sharedPref =
            _ctx.GetSharedPreferences(_ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        var edit = sharedPref.Edit();
        edit.PutBoolean("send_notification_on_dxcc", args.IsChecked);
        edit.Apply();
        if (_ctx.CheckSelfPermission(Manifest.Permission.PostNotifications) != Permission.Granted)
        {
            ((Activity)_ctx).RequestPermissions(new[] {Manifest.Permission.PostNotifications }, 645);
        }
    }

    public void VibrateDxccCheckboxChanged(object? o, CompoundButton.CheckedChangeEventArgs args)
    {
        SettingsVariables.VibrateOnDxcc = args.IsChecked;
        // save conf
        var sharedPref =
            _ctx.GetSharedPreferences(_ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        var edit = sharedPref.Edit();
        edit.PutBoolean("vibrate_on_dxcc", args.IsChecked);
        edit.Apply();
        if (_ctx.CheckSelfPermission(Manifest.Permission.Vibrate) != Permission.Granted)
        {
            ((Activity)_ctx).RequestPermissions(new[] {Manifest.Permission.Vibrate }, 645);
        }
    }
}