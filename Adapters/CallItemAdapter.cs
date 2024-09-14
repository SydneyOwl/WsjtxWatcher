using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Graphics;
using Android.Views;
using WsjtxWatcher.Ft8Transmit;
using WsjtxWatcher.Variables;
using WsjtxWatcher.ViewModels;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Adapters;

public class CallItemAdapter : ArrayAdapter<DecodedMsg>
{
    private readonly Context _context;
    private readonly MainViewModel _model = MainViewModel.GetInstance();

    public CallItemAdapter(Context context, int textViewResourceId, IList<DecodedMsg> objects)
        : base(context, textViewResourceId, objects)
    {
        _context = context;
    }

    public override View GetView(int position, View convertView, ViewGroup parent)
    {
        if (convertView == null)
        {
            var inflater = LayoutInflater.From(_context);
            convertView = inflater.Inflate(ResourceConstant.Layout.call_item, parent, false);

            convertView.Tag = new ViewHolder
            {
                CallingListIdBTextView = convertView.FindViewById<TextView>(ResourceConstant.Id.callingListIdBTextView),
                CallListDtTextView = convertView.FindViewById<TextView>(ResourceConstant.Id.callListDtTextView),
                CallingListFreqTextView =
                    convertView.FindViewById<TextView>(ResourceConstant.Id.callingListFreqTextView),
                CallListMessageTextView =
                    convertView.FindViewById<TextView>(ResourceConstant.Id.callListMessageTextView),
                BandItemTextView = convertView.FindViewById<TextView>(ResourceConstant.Id.bandItemTextView),
                CallingUtcTextView = convertView.FindViewById<TextView>(ResourceConstant.Id.callingUtcTextView),
                LowTrustTextview = convertView.FindViewById<TextView>(ResourceConstant.Id.lowTrustTextview),
                CallToItemTextView = convertView.FindViewById<TextView>(ResourceConstant.Id.callToItemTextView),
                CallFromItemTextView = convertView.FindViewById<TextView>(ResourceConstant.Id.CallFromItemTextView),
                CallingListDistTextView =
                    convertView.FindViewById<TextView>(ResourceConstant.Id.callingListDistTextView)
            };
        }

        var holder = (ViewHolder)convertView.Tag;
        var msg = GetItem(position);

        var isUserTransmit = msg.Transmitter == "USER_TRANSMIT";

        holder.CallingListIdBTextView.Visibility = isUserTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.CallListDtTextView.Visibility = isUserTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.CallingListFreqTextView.Visibility = isUserTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.BandItemTextView.Visibility = isUserTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.CallingUtcTextView.Visibility = isUserTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.LowTrustTextview.Visibility = isUserTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.CallToItemTextView.Visibility = isUserTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.CallFromItemTextView.Visibility = isUserTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.CallingListDistTextView.Visibility = isUserTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.CallListMessageTextView.Text = isUserTransmit
            ? string.IsNullOrEmpty(msg.Message)
                ? _context.GetString(ResourceConstant.String.user_tx_period)
                : msg.Message
            : msg.Message;

        if (!isUserTransmit)
        {
            holder.CallingListIdBTextView.Text = msg.Snr.ToString();
            holder.CallListDtTextView.Text = msg.OffsetTimeSeconds.ToString("F1");
            holder.CallingListFreqTextView.Text = msg.OffsetFrequencyHz.ToString();
            holder.CallingUtcTextView.Text = msg.DecodeTime;
            holder.LowTrustTextview.Visibility = msg.LowConfidence ? ViewStates.Visible : ViewStates.Invisible;
            holder.CallToItemTextView.Text = SettingsVariables.CurrentLanguage == "zh"
                ? msg.ToLocationCountryZh
                : msg.ToLocationCountryEn;
            holder.CallFromItemTextView.Text = SettingsVariables.CurrentLanguage == "zh"
                ? msg.FromLocationCountryZh
                : msg.FromLocationCountryEn;
            holder.CallingListDistTextView.Text = msg.Distance;
            holder.BandItemTextView.Text =
                _model.CurrentFreq == 0 ? "未知" : (_model.CurrentFreq / 1_000_000).ToString("F3") + "MHz";

            var messageColor = msg.Message.Contains("RR73") || msg.Message.Contains("73") || msg.Message.Contains("RRR")
                ? ResourceConstant.Color.tracker_new_cq_win_end_color
                : ResourceConstant.Color.text_view_color;
            holder.CallListMessageTextView.SetTextColor(Context.Resources.GetColor(messageColor));
            holder.CallListMessageTextView.PaintFlags = msg.Message.Contains("RR73") || msg.Message.Contains("73") ||
                                                        msg.Message.Contains("RRR")
                ? PaintFlags.StrikeThruText
                : PaintFlags.LinearText;

            if (!string.IsNullOrEmpty(SettingsVariables.MyCallsign) &&
                msg.Message.Contains(SettingsVariables.MyCallsign))
                holder.CallListMessageTextView.SetTextColor(
                    Context.Resources.GetColor(ResourceConstant.Color.message_in_my_call_text_color));

            var sec = int.Parse(msg.DecodeTime.Split(":").Last());
            var backgroundColor = sec is > 55 and < 60 || sec < 5 || sec is > 25 and < 35
                ? ResourceConstant.Color.odd_period
                : ResourceConstant.Color.even_period;
            convertView.SetBackgroundColor(Context.Resources.GetColor(backgroundColor));
        }

        return convertView;
    }

    private class ViewHolder : Object
    {
        public TextView CallingListIdBTextView { get; set; }
        public TextView CallListDtTextView { get; set; }
        public TextView CallingListFreqTextView { get; set; }
        public TextView CallListMessageTextView { get; set; }
        public TextView BandItemTextView { get; set; }
        public TextView CallingUtcTextView { get; set; }
        public TextView LowTrustTextview { get; set; }
        public TextView CallToItemTextView { get; set; }
        public TextView CallFromItemTextView { get; set; }
        public TextView CallingListDistTextView { get; set; }
    }
}