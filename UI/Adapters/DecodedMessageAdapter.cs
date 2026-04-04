using System.Collections;
using System.Collections.Specialized;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Text.Style;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.UI.Adapters;

public sealed class DecodedMessageAdapter : RecyclerView.Adapter
{
    private readonly Dictionary<(int FillColor, int StrokeColor), Drawable.ConstantState?> _backgroundCache = new();
    private readonly Context _context;
    private readonly Action<int, View> _itemLongClick;
    private readonly Func<AppSettings> _settingsProvider;
    private readonly IList<DecodedRadioMessage> _source;
    private List<DecodedRadioMessage> _filteredItems = [];
    private string _query = string.Empty;

    public DecodedMessageAdapter(
        Context context,
        IList<DecodedRadioMessage> source,
        Func<AppSettings> settingsProvider,
        Action<int, View> itemLongClick)
    {
        _context = context;
        _source = source;
        _settingsProvider = settingsProvider;
        _itemLongClick = itemLongClick;
        ApplyFilter(string.Empty);

        if (source is INotifyCollectionChanged notifyCollectionChanged)
        {
            notifyCollectionChanged.CollectionChanged += OnCollectionChanged;
        }
    }

    public DecodedRadioMessage this[int position] => _filteredItems[position];

    public int Count => _filteredItems.Count;

    public override int ItemCount => _filteredItems.Count;

    public override long GetItemId(int position)
    {
        return position;
    }

    public void ApplyFilter(string query)
    {
        _query = (query ?? string.Empty).Trim().ToUpperInvariant();
        _filteredItems = string.IsNullOrWhiteSpace(_query)
            ? [.. _source]
            : _source.Where(item =>
                    item.Transmitter.Contains(_query, StringComparison.OrdinalIgnoreCase) ||
                    item.Receiver.Contains(_query, StringComparison.OrdinalIgnoreCase))
                .ToList();
        NotifyDataSetChanged();
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var inflater = LayoutInflater.From(_context) ?? throw new InvalidOperationException("Failed to create layout inflater.");
        var view = inflater.Inflate(Resource.Layout.call_item, parent, false)
            ?? throw new InvalidOperationException("Failed to inflate decoded message row.");
        return new ViewHolder(view, _itemLongClick);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        BindViewHolder((ViewHolder)holder, _filteredItems[position]);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _source is INotifyCollectionChanged notifyCollectionChanged)
        {
            notifyCollectionChanged.CollectionChanged -= OnCollectionChanged;
        }

        base.Dispose(disposing);
    }

    private void BindViewHolder(ViewHolder holder, DecodedRadioMessage message)
    {
        var settings = _settingsProvider();
        var isIgnored = IgnoredCallsignMatcher.IsIgnored(message, settings.IgnoredCallsigns, settings.IgnoredCallsignMatchTarget);
        var languageCode = Java.Util.Locale.Default?.Language ?? "en";
        var isCompactMessage = message.IsUserTransmit || message.IsSystemNotice;
        var displayMessage = message.IsUserTransmit
            ? FormatTransmitMessage(message)
            : message.Message;

        holder.Snr.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.DeltaTime.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.Offset.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.Band.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.Utc.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.ToCountry.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.FromCountry.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;
        holder.Distance.Visibility = isCompactMessage ? ViewStates.Gone : ViewStates.Visible;

        holder.Message.PaintFlags = PaintFlags.LinearText;
        holder.Message.SetTextColor(GetColor(Resource.Color.text_view_color));
        holder.Message.TextFormatted = isIgnored
            ? new Java.Lang.String(displayMessage)
            : BuildMessageText(displayMessage, message, settings);

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
            holder.Message.SetTextColor(GetColor(isIgnored
                ? Resource.Color.ignored_message_text
                : Resource.Color.text_view_color));

            if (isIgnored)
            {
                holder.Message.PaintFlags |= PaintFlags.StrikeThruText;
            }
        }
        else
        {
            holder.LowConfidence.TextFormatted = new Java.Lang.String(string.Empty);
            holder.LowConfidence.Visibility = ViewStates.Gone;
            if (message.IsSystemNotice)
            {
                holder.Message.SetTextColor(GetColor(Resource.Color.fromcall_is_qso_text_color));
            }
        }

        ApplyRowBackground(holder.Root, message, settings);
        holder.Message.Background = null;
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

    private Java.Lang.ICharSequence BuildMessageText(string text, DecodedRadioMessage message, AppSettings settings)
    {
        if (message.IsUserTransmit || message.IsSystemNotice)
        {
            return new Java.Lang.String(text);
        }

        var shouldBoldMessage = IsCompletedQsoMessage(message) || IsCqMessage(message);
        var callsignRanges = message.ContainsMyCallsign
            ? CallsignPatternMatcher.FindCallsignTokenRanges(text, settings.MyCallsign)
            : [];

        if (!shouldBoldMessage && callsignRanges.Count == 0 && ResolveMessageHighlightColor(message, settings) is null)
        {
            return new Java.Lang.String(text);
        }

        var builder = new SpannableStringBuilder(text);
        var highlightColor = ResolveMessageHighlightColor(message, settings);
        if (highlightColor.HasValue)
        {
            builder.SetSpan(
                new BackgroundColorSpan(GetColor(highlightColor.Value)),
                0,
                text.Length,
                SpanTypes.ExclusiveExclusive);
        }

        if (shouldBoldMessage)
        {
            builder.SetSpan(
                new StyleSpan(TypefaceStyle.Bold),
                0,
                text.Length,
                SpanTypes.ExclusiveExclusive);
        }

        foreach (var (start, length) in callsignRanges)
        {
            builder.SetSpan(
                new ForegroundColorSpan(GetColor(Resource.Color.decoded_message_my_callsign_text)),
                start,
                start + length,
                SpanTypes.ExclusiveExclusive);
            builder.SetSpan(
                new StyleSpan(TypefaceStyle.Bold),
                start,
                start + length,
                SpanTypes.ExclusiveExclusive);
        }

        return builder;
    }

    private void ApplyRowBackground(View rowView, DecodedRadioMessage message, AppSettings settings)
    {
        var (fillColor, strokeColor) = ResolveRowColors(message, settings);
        rowView.Background = GetRowBackground(fillColor, strokeColor);
    }

    private (int FillColor, int StrokeColor) ResolveRowColors(DecodedRadioMessage message, AppSettings settings)
    {
        if (message.IsSystemNotice)
        {
            return (Resource.Color.system_notice_period, Resource.Color.system_notice_period_stroke);
        }

        if (message.IsUserTransmit)
        {
            return (Resource.Color.my_transmit_period, Resource.Color.my_transmit_period_stroke);
        }

        if (IgnoredCallsignMatcher.IsIgnored(message, settings.IgnoredCallsigns, settings.IgnoredCallsignMatchTarget))
        {
            return IsOddPeriod(message.DecodeTimeUtc)
                ? (Resource.Color.odd_period, Resource.Color.odd_period_stroke)
                : (Resource.Color.even_period, Resource.Color.even_period_stroke);
        }

        return IsOddPeriod(message.DecodeTimeUtc)
            ? (Resource.Color.odd_period, Resource.Color.odd_period_stroke)
            : (Resource.Color.even_period, Resource.Color.even_period_stroke);
    }

    private Drawable GetRowBackground(int fillColor, int strokeColor)
    {
        if (_backgroundCache.TryGetValue((fillColor, strokeColor), out var cachedState) && cachedState is not null)
        {
            return cachedState.NewDrawable().Mutate();
        }

        var drawable = CreateRowBackground(fillColor, strokeColor);
        _backgroundCache[(fillColor, strokeColor)] = drawable.GetConstantState();
        return drawable;
    }

    private GradientDrawable CreateRowBackground(int fillColor, int strokeColor)
    {
        var drawable = new GradientDrawable();
        drawable.SetShape(ShapeType.Rectangle);
        drawable.SetColor(GetColor(fillColor));
        drawable.SetStroke(Dp(1), GetColor(strokeColor));
        drawable.SetCornerRadius(Dp(6));
        return drawable;
    }

    private static bool IsAnyMessageHighlightEnabled(AppSettings settings)
    {
        return settings.NotifyOnAnyMessage || settings.VibrateOnAnyMessage;
    }

    private static bool IsWatchedCallsignHighlightEnabled(AppSettings settings)
    {
        return settings.NotifyOnMyCall || settings.VibrateOnMyCall;
    }

    private static bool IsDxccHighlightEnabled(AppSettings settings)
    {
        return settings.NotifyOnSelectedDxcc || settings.VibrateOnSelectedDxcc;
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
        if (!string.IsNullOrWhiteSpace(_query))
        {
            ApplyFilter(_query);
            return;
        }

        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add when e.NewItems is not null && e.NewStartingIndex >= 0:
                InsertItems(e.NewStartingIndex, e.NewItems);
                return;
            case NotifyCollectionChangedAction.Remove when e.OldItems is not null && e.OldStartingIndex >= 0:
                RemoveItems(e.OldStartingIndex, e.OldItems.Count);
                return;
            case NotifyCollectionChangedAction.Reset:
                _filteredItems = [.. _source];
                NotifyDataSetChanged();
                return;
            default:
                ApplyFilter(_query);
                return;
        }
    }

    private void InsertItems(int startIndex, IList items)
    {
        var insertIndex = Math.Clamp(startIndex, 0, _filteredItems.Count);
        for (var index = 0; index < items.Count; index++)
        {
            if (items[index] is DecodedRadioMessage message)
            {
                _filteredItems.Insert(insertIndex + index, message);
            }
        }

        NotifyItemRangeInserted(insertIndex, items.Count);
    }

    private void RemoveItems(int startIndex, int count)
    {
        if (count <= 0 || startIndex < 0 || startIndex >= _filteredItems.Count)
        {
            ApplyFilter(_query);
            return;
        }

        var removeCount = Math.Min(count, _filteredItems.Count - startIndex);
        _filteredItems.RemoveRange(startIndex, removeCount);
        NotifyItemRangeRemoved(startIndex, removeCount);
    }

    private static int? ResolveMessageHighlightColor(DecodedRadioMessage message, AppSettings settings)
    {
        var matchesWatchedCallsign = message.MatchesWatchedCallsignPattern && IsWatchedCallsignHighlightEnabled(settings);
        var matchesDxcc = message.MatchesSelectedDxcc && IsDxccHighlightEnabled(settings);
        if (matchesWatchedCallsign && matchesDxcc)
        {
            return Resource.Color.decoded_match_multi_fill;
        }

        if (matchesWatchedCallsign)
        {
            return Resource.Color.decoded_match_callsign_fill;
        }

        if (matchesDxcc)
        {
            return Resource.Color.decoded_match_dxcc_fill;
        }

        if (IsAnyMessageHighlightEnabled(settings))
        {
            return Resource.Color.decoded_match_any_fill;
        }

        return null;
    }

    private static bool IsCompletedQsoMessage(DecodedRadioMessage message)
    {
        return message.Message.Contains("RR73", StringComparison.OrdinalIgnoreCase) ||
               message.Message.Contains(" RRR", StringComparison.OrdinalIgnoreCase) ||
               message.Message.EndsWith(" 73", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCqMessage(DecodedRadioMessage message)
    {
        return message.Message.StartsWith("CQ", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOddPeriod(string decodeTime)
    {
        if (decodeTime.Length < 2 || !int.TryParse(decodeTime[^2..], out var seconds))
        {
            return false;
        }

        return seconds is > 55 and < 60 or < 5 or > 25 and < 35;
    }

    private int Dp(int value)
    {
        var density = _context.Resources?.DisplayMetrics?.Density ?? 1f;
        return Math.Max(1, (int)Math.Round(value * density));
    }

    private Color GetColor(int colorResource)
    {
        return new Color(_context.GetColor(colorResource));
    }

    private sealed class ViewHolder : RecyclerView.ViewHolder
    {
        public ViewHolder(View root, Action<int, View> itemLongClick) : base(root)
        {
            Root = root;
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

            root.LongClick += (_, _) =>
            {
                var position = BindingAdapterPosition;
                if (position != RecyclerView.NoPosition)
                {
                    itemLongClick(position, root);
                }
            };
        }

        public View Root { get; }
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
