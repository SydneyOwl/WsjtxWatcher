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
      // if (args.IsChecked)
      // {
      //        NotificationManager manager = NotificationManager.FromContext(ctx);
      //        var isNotifycationEnabled = manager.AreNotificationsEnabled();
      //        if (!isNotifycationEnabled)
      //        {
      //           Intent intent = new Intent(Android.Provider.Settings.ActionNotificationPolicyAccessSettings);
      //           ctx.StartActivity(intent);
      //        }
      // }
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
}