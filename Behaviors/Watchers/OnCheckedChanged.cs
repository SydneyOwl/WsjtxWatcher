using _Microsoft.Android.Resource.Designer;
using Android.Content;
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

        NotificationManager.FromContext(_ctx).AreNotificationsEnabled();
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
    }
}