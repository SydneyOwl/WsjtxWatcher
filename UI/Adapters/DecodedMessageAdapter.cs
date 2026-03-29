using System.Collections.Specialized;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Views;
using Android.Widget;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.UI.Adapters;

public sealed class DecodedMessageAdapter : BaseAdapter<DecodedRadioMessage>
{
    private readonly Context _context;
    private readonly IList<DecodedRadioMessage> _source;
    private readonly Func<AppSettings> _settingsProvider;
    private List<DecodedRadioMessage> _filteredItems = new();
    private string _query = string.Empty;

    public DecodedMessageAdapter(Context context, IList<DecodedRadioMessage> source, Func<AppSettings> settingsProvider)
    {
        _context = context;
        _source = source;
        _settingsProvider = settingsProvider;
        ApplyFilter(string.Empty);

        if (source is INotifyCollectionChanged notifyCollectionChanged)
        {
            notifyCollectionChanged.CollectionChanged += OnCollectionChanged;
        }
    }

    public override DecodedRadioMessage this[int position] => _filteredItems[position];

    public override int Count => _filteredItems.Count;

    public override long GetItemId(int position)
    {
        return position;
    }

    public void ApplyFilter(string query)
    {
        _query = (query ?? string.Empty).Trim().ToUpperInvariant();
        _filteredItems = string.IsNullOrWhiteSpace(_query)
            ? _source.ToList()
            : _source.Where(item =>
                    item.Transmitter.Contains(_query, StringComparison.OrdinalIgnoreCase) ||
                    item.Receiver.Contains(_query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        NotifyDataSetChanged();
    }

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        var inflater = LayoutInflater.From(_context) ?? throw new InvalidOperationException("Failed to create layout inflater.");
        var view = convertView ?? inflater.Inflate(Resource.Layout.call_item, parent, false)
            ?? throw new InvalidOperationException("Failed to inflate decoded message row.");
        var holder = view.Tag as ViewHolder ?? new ViewHolder(view);
        view.Tag = holder;

        var message = _filteredItems[position];
        var settings = _settingsProvider();
        var languageCode = Java.Util.Locale.Default?.Language ?? "en";
        var isTransmit = message.IsUserTransmit;

        holder.Snr.Visibility = isTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.DeltaTime.Visibility = isTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.Offset.Visibility = isTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.Band.Visibility = isTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.Utc.Visibility = isTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.LowConfidence.Visibility = isTransmit || !message.LowConfidence ? ViewStates.Invisible : ViewStates.Visible;
        holder.ToCountry.Visibility = isTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.FromCountry.Visibility = isTransmit ? ViewStates.Gone : ViewStates.Visible;
        holder.Distance.Visibility = isTransmit ? ViewStates.Gone : ViewStates.Visible;

        holder.Message.Text = isTransmit
            ? string.IsNullOrWhiteSpace(message.Message) ? _context.GetString(Resource.String.user_tx_period) : message.Message
            : message.Message;

        holder.Message.PaintFlags = PaintFlags.LinearText;
        holder.Message.SetTextColor(GetColor(Resource.Color.text_view_color));

        if (!isTransmit)
        {
            holder.Snr.Text = message.Snr.ToString();
            holder.DeltaTime.Text = message.OffsetTimeSeconds.ToString("F1");
            holder.Offset.Text = message.OffsetFrequencyHz.ToString();
            holder.Utc.Text = message.DecodeTimeUtc;
            holder.ToCountry.Text = languageCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? message.ToCountryChinese
                : message.ToCountryEnglish;
            holder.FromCountry.Text = languageCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase)
                ? message.FromCountryChinese
                : message.FromCountryEnglish;
            holder.Distance.Text = message.DistanceText;
            holder.Band.Text = message.DialFrequencyHz > 0d
                ? $"{message.DialFrequencyHz / 1_000_000d:F3}MHz"
                : _context.GetString(Resource.String.unknown_band);

            if (message.Message.Contains("RR73", StringComparison.OrdinalIgnoreCase) ||
                message.Message.Contains(" RRR", StringComparison.OrdinalIgnoreCase) ||
                message.Message.EndsWith(" 73", StringComparison.OrdinalIgnoreCase))
            {
                holder.Message.PaintFlags |= PaintFlags.StrikeThruText;
                holder.Message.SetTextColor(GetColor(Resource.Color.tracker_new_cq_win_end_color));
            }

            if (!string.IsNullOrWhiteSpace(settings.MyCallsign) &&
                message.Message.Contains(settings.MyCallsign, StringComparison.OrdinalIgnoreCase))
            {
                holder.Message.SetTextColor(GetColor(Resource.Color.message_in_my_call_text_color));
            }

            view.SetBackgroundColor(GetColor(GetRowColor(message.DecodeTimeUtc)));
        }
        else
        {
            view.SetBackgroundColor(GetColor(Resource.Color.my_transmit_period));
        }

        return view;
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ApplyFilter(_query);
    }

    private static int GetRowColor(string decodeTime)
    {
        if (decodeTime.Length < 2 || !int.TryParse(decodeTime[^2..], out var seconds))
        {
            return Resource.Color.even_period;
        }

        return seconds is > 55 and < 60 or < 5 or > 25 and < 35
            ? Resource.Color.odd_period
            : Resource.Color.even_period;
    }

    private Color GetColor(int colorResource)
    {
        return new Color(_context.GetColor(colorResource));
    }

    private sealed class ViewHolder : Java.Lang.Object
    {
        public ViewHolder(View root)
        {
            Snr = root.FindViewById<TextView>(Resource.Id.callingListIdBTextView)!;
            DeltaTime = root.FindViewById<TextView>(Resource.Id.callListDtTextView)!;
            Offset = root.FindViewById<TextView>(Resource.Id.callingListFreqTextView)!;
            Message = root.FindViewById<TextView>(Resource.Id.callListMessageTextView)!;
            Band = root.FindViewById<TextView>(Resource.Id.bandItemTextView)!;
            Utc = root.FindViewById<TextView>(Resource.Id.callingUtcTextView)!;
            LowConfidence = root.FindViewById<TextView>(Resource.Id.lowTrustTextview)!;
            ToCountry = root.FindViewById<TextView>(Resource.Id.callToItemTextView)!;
            FromCountry = root.FindViewById<TextView>(Resource.Id.CallFromItemTextView)!;
            Distance = root.FindViewById<TextView>(Resource.Id.callingListDistTextView)!;
        }

        public TextView Snr { get; }
        public TextView DeltaTime { get; }
        public TextView Offset { get; }
        public TextView Message { get; }
        public TextView Band { get; }
        public TextView Utc { get; }
        public TextView LowConfidence { get; }
        public TextView ToCountry { get; }
        public TextView FromCountry { get; }
        public TextView Distance { get; }
    }
}
