using System.Collections.Specialized;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using WsjtxUtils.WsjtxMessages.Messages;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

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
        var isCompactMessage = message.IsUserTransmit || message.IsSystemNotice;

        holder.Snr.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.DeltaTime.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.Offset.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.Band.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.Utc.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.ToCountry.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.FromCountry.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.Distance.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;

        holder.Message.Text = isTransmit
            ? FormatTransmitMessage(message)
            : message.Message;

        holder.Message.PaintFlags = PaintFlags.LinearText;
        holder.Message.SetTextColor(GetColor(Resource.Color.text_view_color));

        if (!isCompactMessage)
        {
            holder.Snr.Text = message.Snr.ToString();
            holder.DeltaTime.Text = message.OffsetTimeSeconds.ToString("F1");
            holder.Offset.Text = message.OffsetFrequencyHz.ToString();
            holder.Utc.Text = message.DecodeTimeUtc;
            holder.ToCountry.Text = GetCountryName(languageCode, message.ToCountryEnglish, message.ToCountryChinese);
            holder.FromCountry.Text = GetCountryName(languageCode, message.FromCountryEnglish, message.FromCountryChinese);
            holder.Distance.Text = message.DistanceText;
            ApplyModeStatus(holder.LowConfidence, message);
            holder.Band.Text = FormatFrequency(message);

            if (message.Message.Contains("RR73", StringComparison.OrdinalIgnoreCase) ||
                message.Message.Contains(" RRR", StringComparison.OrdinalIgnoreCase) ||
                message.Message.EndsWith(" 73", StringComparison.OrdinalIgnoreCase))
            {
                holder.Message.PaintFlags |= PaintFlags.StrikeThruText;
                holder.Message.SetTextColor(GetColor(Resource.Color.tracker_new_cq_win_end_color));
            }

            if (CallsignPatternMatcher.IsMatch(message.Message, settings.WatchedCallsignPatterns))
            {
                holder.Message.SetTextColor(GetColor(Resource.Color.message_in_my_call_text_color));
            }

            view.SetBackgroundColor(GetColor(GetRowColor(message.DecodeTimeUtc)));
        }
        else
        {
            holder.LowConfidence.TextFormatted = new Java.Lang.String(string.Empty);
            holder.LowConfidence.Visibility = ViewStates.Gone;
            if (message.IsSystemNotice)
            {
                holder.Message.SetTextColor(GetColor(Resource.Color.fromcall_is_qso_text_color));
                view.SetBackgroundColor(GetColor(Resource.Color.system_notice_period));
            }
            else
            {
                view.SetBackgroundColor(GetColor(Resource.Color.my_transmit_period));
            }
        }

        return view;
    }

    private static string GetCountryName(string languageCode, string englishName, string chineseName)
    {
        return languageCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(chineseName)
            ? chineseName
            : englishName;
    }

    private string FormatFrequency(DecodedRadioMessage message)
    {
        var frequency = message.DialFrequencyHz > 0d
            ? $"{message.DialFrequencyHz / 1_000_000d:F3}MHz"
            : string.Empty;

        return !string.IsNullOrWhiteSpace(frequency)
            ? frequency
            : _context.GetString(Resource.String.unknown_band);
    }

    private string FormatTransmitMessage(DecodedRadioMessage message)
    {
        if (!string.IsNullOrWhiteSpace(message.Message))
        {
            return message.Message;
        }

        var fallback = _context.GetString(Resource.String.user_tx_period);
        return string.IsNullOrWhiteSpace(message.Mode)
            ? fallback
            : $"{message.Mode.ToUpperInvariant()} {fallback}";
    }

    private void ApplyModeStatus(TextView target, DecodedRadioMessage message)
    {
        var mode = (message.Mode ?? string.Empty).Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(mode) && !message.LowConfidence)
        {
            target.TextFormatted = new Java.Lang.String(string.Empty);
            target.Visibility = ViewStates.Invisible;
            return;
        }
        
        // translate mode
        mode = WsjtxMessageParser.DecodeModeNotationsToString(mode);

        var builder = new SpannableStringBuilder();
        if (!string.IsNullOrWhiteSpace(mode))
        {
            var modeStart = builder.Length();
            builder.Append(mode);
            builder.SetSpan(
                new ForegroundColorSpan(GetColor(GetModeColor(mode))),
                modeStart,
                builder.Length(),
                SpanTypes.ExclusiveExclusive);
        }

        if (message.LowConfidence)
        {
            if (builder.Length() > 0)
            {
                builder.Append(" ");
            }

            var lbStart = builder.Length();
            builder.Append("LB");
            builder.SetSpan(
                new ForegroundColorSpan(GetColor(Resource.Color.mode_lb_color)),
                lbStart,
                builder.Length(),
                SpanTypes.ExclusiveExclusive);
        }

        target.TextFormatted = builder;
        target.Visibility = ViewStates.Visible;
    }

    private static int GetModeColor(string mode)
    {
        return mode switch
        {
            "FT8" => Resource.Color.mode_ft8_color,
            "FT4" => Resource.Color.mode_ft4_color,
            "JT9" => Resource.Color.mode_jt9_color,
            "Q65" => Resource.Color.mode_q65_color,
            "WSPR" => Resource.Color.mode_wspr_color,
            _ => Resource.Color.mode_default_color
        };
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
