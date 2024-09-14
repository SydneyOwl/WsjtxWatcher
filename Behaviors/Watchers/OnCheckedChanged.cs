using _Microsoft.Android.Resource.Designer;
using Android.Content;
using WsjtxWatcher.Variables;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnCheckedChanged
{
   private Context ctx;
   public OnCheckedChanged(Context ctx)
   {
      this.ctx = ctx;
   }
   public void SendNotificationCheckboxChanged(object? o, CompoundButton.CheckedChangeEventArgs args)
   {
      SettingsVariables.send_notification_on_call = args.IsChecked;
      // save conf
      var sharedPref =
         ctx.GetSharedPreferences(ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
      var edit = sharedPref.Edit();
      edit.PutBoolean("send_notification_on_call", args.IsChecked);
      edit.Apply();
      
      NotificationManager.FromContext(ctx).AreNotificationsEnabled();
   }
   public void VibrateCheckboxChanged(object? o, CompoundButton.CheckedChangeEventArgs args)
   {
      SettingsVariables.vibrate_on_call = args.IsChecked;
      // save conf
      var sharedPref =
         ctx.GetSharedPreferences(ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
      var edit = sharedPref.Edit();
      edit.PutBoolean("vibrate_on_call", args.IsChecked);
      edit.Apply();
   }
   
   public void SendNotificationAllCheckboxChanged(object? o, CompoundButton.CheckedChangeEventArgs args)
   {
      SettingsVariables.send_notification_on_all = args.IsChecked;
      // save conf
      var sharedPref =
         ctx.GetSharedPreferences(ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
      var edit = sharedPref.Edit();
      edit.PutBoolean("send_notification_on_all", args.IsChecked);
      edit.Apply();
   }
   
   public void VibrateAllCheckboxChanged(object? o, CompoundButton.CheckedChangeEventArgs args)
   {
      SettingsVariables.vibrate_on_all = args.IsChecked;
      // save conf
      var sharedPref =
         ctx.GetSharedPreferences(ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
      var edit = sharedPref.Edit();
      edit.PutBoolean("vibrate_on_all", args.IsChecked);
      edit.Apply();
   }
   
   public void SendNotificationDxccCheckboxChanged(object? o, CompoundButton.CheckedChangeEventArgs args)
   {
      SettingsVariables.send_notification_on_dxcc = args.IsChecked;
      // save conf
      var sharedPref =
         ctx.GetSharedPreferences(ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
      var edit = sharedPref.Edit();
      edit.PutBoolean("send_notification_on_dxcc", args.IsChecked);
      edit.Apply();
   }
   
   public void VibrateDxccCheckboxChanged(object? o, CompoundButton.CheckedChangeEventArgs args)
   {
      SettingsVariables.vibrate_on_dxcc = args.IsChecked;
      // save conf
      var sharedPref =
         ctx.GetSharedPreferences(ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
      var edit = sharedPref.Edit();
      edit.PutBoolean("vibrate_on_dxcc", args.IsChecked);
      edit.Apply();
   }
}