using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using WsjtxWatcher.Ft8Transmit;
using WsjtxWatcher.Variables;
using WsjtxWatcher.ViewModels;

namespace WsjtxWatcher.Adapters
{
    public class CallItemAdapter : ArrayAdapter<DecodedMsg>
    {
        private readonly MainViewModel _model = MainViewModel.GetInstance();
        private readonly Context _context;

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
                convertView = inflater.Inflate(Resource.Layout.call_item, parent, false);

                convertView.Tag = new ViewHolder
                {
                    CallingListIdBTextView = convertView.FindViewById<TextView>(Resource.Id.callingListIdBTextView),
                    CallListDtTextView = convertView.FindViewById<TextView>(Resource.Id.callListDtTextView),
                    CallingListFreqTextView = convertView.FindViewById<TextView>(Resource.Id.callingListFreqTextView),
                    CallListMessageTextView = convertView.FindViewById<TextView>(Resource.Id.callListMessageTextView),
                    BandItemTextView = convertView.FindViewById<TextView>(Resource.Id.bandItemTextView),
                    CallingUtcTextView = convertView.FindViewById<TextView>(Resource.Id.callingUtcTextView),
                    LowTrustTextview = convertView.FindViewById<TextView>(Resource.Id.lowTrustTextview),
                    CallToItemTextView = convertView.FindViewById<TextView>(Resource.Id.callToItemTextView),
                    CallFromItemTextView = convertView.FindViewById<TextView>(Resource.Id.CallFromItemTextView),
                    CallingListDistTextView = convertView.FindViewById<TextView>(Resource.Id.callingListDistTextView)
                };
            }

            var holder = (ViewHolder)convertView.Tag;
            var msg = GetItem(position);

            bool isUserTransmit = msg.Transmitter == "USER_TRANSMIT";

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
                ? string.IsNullOrEmpty(msg.Message) ? _context.GetString(Resource.String.user_tx_period) : msg.Message 
                : msg.Message;

            if (!isUserTransmit)
            {
                holder.CallingListIdBTextView.Text = msg.Snr.ToString();
                holder.CallListDtTextView.Text = msg.OffsetTimeSeconds.ToString("F1");
                holder.CallingListFreqTextView.Text = msg.OffsetFrequencyHz.ToString();
                holder.CallingUtcTextView.Text = msg.DecodeTime;
                holder.LowTrustTextview.Visibility = msg.LowConfidence ? ViewStates.Visible : ViewStates.Invisible;
                holder.CallToItemTextView.Text = SettingsVariables.currentLanguage == "zh" ? msg.ToLocationCountryZh : msg.ToLocationCountryEn;
                holder.CallFromItemTextView.Text = SettingsVariables.currentLanguage == "zh" ? msg.FromLocationCountryZh : msg.FromLocationCountryEn;
                holder.CallingListDistTextView.Text = msg.Distance;
                holder.BandItemTextView.Text = _model.currentFreq == 0 ? "未知" : (_model.currentFreq / 1_000_000).ToString("F3") + "MHz";

                var messageColor = msg.Message.Contains("RR73") || msg.Message.Contains("73") || msg.Message.Contains("RRR")
                    ? Resource.Color.tracker_new_cq_win_end_color
                    : Resource.Color.text_view_color;
                holder.CallListMessageTextView.SetTextColor(Context.Resources.GetColor(messageColor));
                holder.CallListMessageTextView.PaintFlags = msg.Message.Contains("RR73") || msg.Message.Contains("73") || msg.Message.Contains("RRR")
                    ? PaintFlags.StrikeThruText
                    : PaintFlags.LinearText;

                if (!string.IsNullOrEmpty(SettingsVariables.myCallsign) && msg.Message.Contains(SettingsVariables.myCallsign))
                {
                    holder.CallListMessageTextView.SetTextColor(Context.Resources.GetColor(Resource.Color.message_in_my_call_text_color));
                }

                int sec = int.Parse(msg.DecodeTime.Split(":").Last());
                var backgroundColor = (sec is > 55 and < 60) || sec < 5 || (sec is > 25 and < 35)
                    ? Resource.Color.odd_period
                    : Resource.Color.even_period;
                convertView.SetBackgroundColor(Context.Resources.GetColor(backgroundColor));
            }

            return convertView;
        }

        private class ViewHolder : Java.Lang.Object
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
}
