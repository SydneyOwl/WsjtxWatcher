using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Microsoft.VisualBasic;
using WsjtxWatcher.Ft8Transmit;
using WsjtxWatcher.Utils.DeviceActions;
using WsjtxWatcher.Variables;
using WsjtxWatcher.ViewModels;

namespace WsjtxWatcher.Adapters;

public class CallItemAdapter : ArrayAdapter<DecodedMsg>
{
    private MainViewModel model = MainViewModel.GetInstance();

    private Context ctx;
    public CallItemAdapter(Context context, int textViewResourceId, IList<DecodedMsg> objects) : base(context, textViewResourceId, objects)
    {
        ctx = context;
    }

    public override View GetView(int position, View convertView, ViewGroup parent)
    {
        DecodedMsg msg = GetItem(position);
        if (convertView == null)
        {
            LayoutInflater inflater = LayoutInflater.From(Context);
            convertView = inflater.Inflate(Resource.Layout.call_item, parent, false);
        }
        var callingListIdBTextView = convertView.FindViewById<TextView>(Resource.Id.callingListIdBTextView);
        var callListDtTextView = convertView.FindViewById<TextView>(Resource.Id.callListDtTextView);
        var callingListFreqTextView = convertView.FindViewById<TextView>(Resource.Id.callingListFreqTextView);
        var callListMessageTextView = convertView.FindViewById<TextView>(Resource.Id.callListMessageTextView);
        var bandItemTextView = convertView.FindViewById<TextView>(Resource.Id.bandItemTextView);
        
        var callingUtcTextView = convertView.FindViewById<TextView>(Resource.Id.callingUtcTextView);
        var lowTrustTextview = convertView.FindViewById<TextView>(Resource.Id.lowTrustTextview);
        var callToItemTextView = convertView.FindViewById<TextView>(Resource.Id.callToItemTextView);
        var callFromItemTextView = convertView.FindViewById<TextView>(Resource.Id.CallFromItemTextView);
        var callingListDistTextView = convertView.FindViewById<TextView>(Resource.Id.callingListDistTextView);

        if (msg.Transmitter == "USER_TRANSMIT")
        {
            if (string.IsNullOrEmpty(msg.Message))
            {
                callListMessageTextView.Text = Context.GetString(Resource.String.user_tx_period);
            }
            else
            {
                callListMessageTextView.Text = msg.Message;
            }
            // 隐藏其他东西
            callingListIdBTextView.Visibility = ViewStates.Gone;
            callListDtTextView.Visibility = ViewStates.Gone;
            callingListFreqTextView.Visibility = ViewStates.Gone; 
            // callListMessageTextView.Visibility = ViewStates.Gone; 
            bandItemTextView.Visibility = ViewStates.Gone;
            callingUtcTextView.Visibility = ViewStates.Gone;
            lowTrustTextview.Visibility = ViewStates.Gone;
            callToItemTextView.Visibility = ViewStates.Gone;
            callFromItemTextView.Visibility = ViewStates.Gone;
            callingListDistTextView.Visibility = ViewStates.Gone;
            return convertView;
        }
        callingListIdBTextView.Visibility = ViewStates.Visible;
        callListDtTextView.Visibility = ViewStates.Visible;
        callingListFreqTextView.Visibility = ViewStates.Visible; 
        // callListMessageTextView.Visibility = ViewStates.Gone; 
        bandItemTextView.Visibility = ViewStates.Visible;
        callingUtcTextView.Visibility = ViewStates.Visible;
        lowTrustTextview.Visibility = ViewStates.Visible;
        callToItemTextView.Visibility = ViewStates.Visible;
        callFromItemTextView.Visibility = ViewStates.Visible;
        callingListDistTextView.Visibility = ViewStates.Visible;

        callingListIdBTextView.Text = msg.Snr.ToString();
        callListDtTextView.Text = msg.OffsetTimeSeconds.ToString("F1");
        callingListFreqTextView.Text = msg.OffsetFrequencyHz.ToString();
        callListMessageTextView.Text = msg.Message;
        callingUtcTextView.Text = msg.DecodeTime;
        lowTrustTextview.Visibility = msg.LowConfidence ? ViewStates.Visible : ViewStates.Invisible;
        callToItemTextView.Text = SettingsVariables.currentLanguage == "zh"
            ? msg.ToLocationCountryZh
            : msg.ToLocationCountryEn;
        callFromItemTextView.Text = SettingsVariables.currentLanguage == "zh"
            ? msg.FromLocationCountryZh
            : msg.FromLocationCountryEn;
        callingListDistTextView.Text = msg.Distance;
        // 转成MHZ
        if (model.currentFreq == 0)
        {
            bandItemTextView.Text = "未知";
        }
        else
        {
            bandItemTextView.Text = (model.currentFreq/1_000_000).ToString("F3") + "MHz";
        }
        
        // 有RR73/RRR/73的话就标红并划掉
        if (msg.Message.Contains("RR73")|| msg.Message.Contains("73") || msg.Message.Contains("RRR"))
        {
            callListMessageTextView.SetTextColor(Context.Resources.GetColor(Resource.Color.tracker_new_cq_win_end_color));
            callListMessageTextView.PaintFlags = PaintFlags.StrikeThruText;
        }
        else
        {
            callListMessageTextView.SetTextColor(Context.Resources.GetColor(Resource.Color.text_view_color));
            callListMessageTextView.PaintFlags = PaintFlags.LinearText;
        }
        
        // new Task(() =>
        // {
        //     if (SettingsVariables.vibrate_on_all) Vibrate.DoVibrate(ctx);
        //     if (SettingsVariables.send_notification_on_all)
        //         Notifications.getInstance(ctx).PopNotification(msg.Message);
        // }).Start();
        // 有我的话就标红
        if (!string.IsNullOrEmpty(SettingsVariables.myCallsign) && msg.Message.Contains(SettingsVariables.myCallsign))
        {
            callListMessageTextView.SetTextColor(Context.Resources.GetColor(Resource.Color.message_in_my_call_text_color));
            // new Task(() =>
            // {
            //     if (SettingsVariables.vibrate_on_call) Vibrate.DoVibrate(ctx);
            //     if (SettingsVariables.send_notification_on_call)
            //         Notifications.getInstance(ctx).PopNotification(msg.Message);
            // }).Start();
        }
        
        // 发射周期背景色设定
        // 计算逻辑： +-5s
        var sec = int.Parse(msg.DecodeTime.Split(":").Last());
        if (((sec is > 55 and <60)|| sec < 5)|| (sec is >25 and <35))
        {
            // 偶数周期6
            //_____|_____0_______________15_____|_____
            convertView.SetBackgroundColor(Context.Resources.GetColor(Resource.Color.odd_period));
        }
        else
        {
            convertView.SetBackgroundColor(Context.Resources.GetColor(Resource.Color.even_period));
        }
        
        return convertView;
    }
}