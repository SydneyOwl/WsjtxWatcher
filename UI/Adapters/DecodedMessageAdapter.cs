using System.Collections;
using System.Collections.Specialized;
using Android.Content;
using Android.Graphics;
using Android.Graphics.Drawables;
using Android.Text;
using Android.Text.Style;
using Android.Util;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using WsjtxWatcher.Core.Models;
using WsjtxWatcher.Core.Utilities;

namespace WsjtxWatcher.UI.Adapters;

public sealed class DecodedMessageAdapter : RecyclerView.Adapter
{
    private static readonly IReadOnlyDictionary<string, string> CompactEnglishCountryNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["Ascension Island"] = "Ascension Is.",
        ["Antigua & Barbuda"] = "Antigua & Barb.",
        ["British Virgin Islands"] = "Br. Virgin Is.",
        ["Central African Republic"] = "Central Afr. Rep.",
        ["Christmas Island"] = "Christmas Is.",
        ["Clipperton Island"] = "Clipperton Is.",
        ["Equatorial Guinea"] = "Eq. Guinea",
        ["Falkland Islands"] = "Falkland Is.",
        ["French Polynesia"] = "Fr. Polynesia",
        ["Galapagos Islands"] = "Galapagos Is.",
        ["Kingdom of Eswatini"] = "Eswatini",
        ["Marshall Islands"] = "Marshall Is.",
        ["North Cook Islands"] = "N. Cook Is.",
        ["Northern Ireland"] = "N. Ireland",
        ["Papua New Guinea"] = "Papua N.G.",
        ["Republic of Korea"] = "S. Korea",
        ["Republic of Kosovo"] = "Kosovo",
        ["Republic of the Congo"] = "Congo Rep.",
        ["Republic of South Sudan"] = "S. Sudan",
        ["Rodriguez Island"] = "Rodriguez Is.",
        ["Sao Tome & Principe"] = "Sao Tome & Pr.",
        ["South Cook Islands"] = "S. Cook Is.",
        ["Trinidad & Tobago"] = "Trinidad & Tob.",
        ["United Nations HQ"] = "UN HQ",
        ["United States"] = "USA",
        ["United Kingdom"] = "UK",
        ["United Arab Emirates"] = "UAE",
        ["US Virgin Islands"] = "US Virgin Is.",
        ["European Russia"] = "EU Russia",
        ["Asiatic Russia"] = "AS Russia",
        ["Bosnia-Herzegovina"] = "Bosnia-Hrzg.",
        ["Dominican Republic"] = "Dom. Rep.",
        ["Czech Republic"] = "Czechia",
        ["Balearic Islands"] = "Balearic Is.",
        ["Canary Islands"] = "Canary Is.",
        ["Cape Verde"] = "Cabo Verde",
        ["West Malaysia"] = "W. Malaysia",
        ["East Malaysia"] = "E. Malaysia",
        ["Sov Mil Order of Malta"] = "SMOM"
    };
    private readonly Dictionary<(int FillColor, int StrokeColor), Drawable.ConstantState?> _backgroundCache = new();
    private readonly Dictionary<(int FillColor, int StrokeColor), Drawable.ConstantState?> _modeBadgeCache = new();
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
        var isIgnored = message.IsIgnored;
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
        holder.Message.SetTextColor(GetColor(Resource.Color.m3_on_surface_variant));
        holder.Message.TextFormatted = isIgnored
            ? new Java.Lang.String(displayMessage)
            : BuildMessageText(displayMessage, message, settings);

        if (!isCompactMessage)
        {
            ApplyCountryTextStyle(holder.ToCountry, languageCode);
            ApplyCountryTextStyle(holder.FromCountry, languageCode);
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
                ? Resource.Color.text_muted
                : Resource.Color.m3_on_surface_variant));

            if (isIgnored)
            {
                holder.Message.PaintFlags |= PaintFlags.StrikeThruText;
            }
        }
        else
        {
            holder.LowConfidence.TextFormatted = new Java.Lang.String(string.Empty);
            holder.LowConfidence.Visibility = ViewStates.Gone;
            holder.LowConfidence.Background = null;
            if (message.IsSystemNotice)
            {
                holder.Message.SetTextColor(GetColor(Resource.Color.m3_primary));
            }
        }

        ApplyRowBackground(holder.Root, message, settings);
        ApplyPeriodIndicator(holder.PeriodIndicator, message);
        holder.Message.Background = null;
    }

    private static string GetCountryName(string languageCode, string englishName, string chineseName)
    {
        return languageCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(chineseName)
            ? chineseName
            : CompactEnglishCountryName(englishName);
    }

    private static string CompactEnglishCountryName(string englishName)
    {
        if (string.IsNullOrWhiteSpace(englishName))
        {
            return string.Empty;
        }

        var normalized = englishName.Trim();
        if (CompactEnglishCountryNames.TryGetValue(normalized, out var compact))
        {
            return compact;
        }

        if (normalized.Length <= 15)
        {
            return normalized;
        }

        var compacted = normalized
            .Replace("Republic of ", "Rep. of ", StringComparison.OrdinalIgnoreCase)
            .Replace("Federated States of ", "FS of ", StringComparison.OrdinalIgnoreCase)
            .Replace("United States of ", "US ", StringComparison.OrdinalIgnoreCase)
            .Replace("Province of ", "", StringComparison.OrdinalIgnoreCase)
            .Replace(" Islands", " Is.", StringComparison.OrdinalIgnoreCase)
            .Replace(" Island", " Is.", StringComparison.OrdinalIgnoreCase)
            .Replace(" Republic", " Rep.", StringComparison.OrdinalIgnoreCase)
            .Replace(" Democratic", " Dem.", StringComparison.OrdinalIgnoreCase)
            .Replace(" Central", " C.", StringComparison.OrdinalIgnoreCase)
            .Replace(" Northern", " N.", StringComparison.OrdinalIgnoreCase)
            .Replace(" Southern", " S.", StringComparison.OrdinalIgnoreCase)
            .Replace(" Eastern", " E.", StringComparison.OrdinalIgnoreCase)
            .Replace(" Western", " W.", StringComparison.OrdinalIgnoreCase)
            .Replace(" French ", " Fr. ", StringComparison.OrdinalIgnoreCase)
            .Replace(" British ", " Br. ", StringComparison.OrdinalIgnoreCase)
            .Replace(" Saint ", " St. ", StringComparison.OrdinalIgnoreCase)
            .Replace(" and ", " & ", StringComparison.OrdinalIgnoreCase);

        return compacted.Length <= 15 ? compacted : compacted;
    }

    private static void ApplyCountryTextStyle(TextView textView, string languageCode)
    {
        textView.SetTextSize(
            ComplexUnitType.Sp,
            languageCode.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? 10f : 9f);
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
            target.Background = null;
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
                new ForegroundColorSpan(GetColor(Resource.Color.mode_lb)),
                lbStart,
                builder.Length(),
                SpanTypes.ExclusiveExclusive);
        }

        target.TextFormatted = builder;
        target.Visibility = ViewStates.Visible;
        ApplyModeBadgeLayout(target, message.LowConfidence);
        ApplyModeBadgeBackground(target, mode, message.LowConfidence);
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
                new ForegroundColorSpan(GetColor(Resource.Color.text_my_call)),
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
        var (fillColor, strokeColor) = ResolveRowColors(message);
        rowView.Background = GetRowBackground(fillColor, strokeColor);
    }

    private void ApplyPeriodIndicator(View indicatorView, DecodedRadioMessage message)
    {
        indicatorView.SetBackgroundColor(GetColor(ResolvePeriodIndicatorColor(message)));
    }

    private (int FillColor, int StrokeColor) ResolveRowColors(DecodedRadioMessage message)
    {
        if (message.IsSystemNotice)
        {
            return (Resource.Color.period_system_fill, Resource.Color.period_system_stroke);
        }

        if (message.IsUserTransmit)
        {
            return (Resource.Color.period_my_tx_fill, Resource.Color.period_my_tx_stroke);
        }

        if (message.IsIgnored)
        {
            return IsOddPeriod(message.DecodeTimeUtc)
                ? (Resource.Color.period_odd_fill, Resource.Color.period_odd_stroke)
                : (Resource.Color.period_even_fill, Resource.Color.period_even_stroke);
        }

        return IsOddPeriod(message.DecodeTimeUtc)
            ? (Resource.Color.period_odd_fill, Resource.Color.period_odd_stroke)
            : (Resource.Color.period_even_fill, Resource.Color.period_even_stroke);
    }

    private static int ResolvePeriodIndicatorColor(DecodedRadioMessage message)
    {
        if (message.IsSystemNotice)
        {
            return Resource.Color.period_system_indicator;
        }

        if (message.IsUserTransmit)
        {
            return Resource.Color.period_my_tx_indicator;
        }

        return IsOddPeriod(message.DecodeTimeUtc)
            ? Resource.Color.period_odd_indicator
            : Resource.Color.period_even_indicator;
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

    private void ApplyModeBadgeBackground(TextView target, string mode, bool lowConfidence)
    {
        var fillColor = ResolveModeBadgeFillColor(mode, lowConfidence);
        var strokeColor = ResolveModeBadgeStrokeColor(mode, lowConfidence);
        target.Background = GetModeBadgeBackground(fillColor, strokeColor);
    }

    private void ApplyModeBadgeLayout(TextView target, bool lowConfidence)
    {
        target.SetMinWidth(0);
        target.SetMinimumWidth(0);
        var horizontalPadding = Dp(lowConfidence ? 6 : 5);
        target.SetPadding(horizontalPadding, Dp(1), horizontalPadding, Dp(1));
    }

    private Drawable GetModeBadgeBackground(int fillColor, int strokeColor)
    {
        if (_modeBadgeCache.TryGetValue((fillColor, strokeColor), out var cachedState) && cachedState is not null)
        {
            return cachedState.NewDrawable().Mutate();
        }

        var drawable = new GradientDrawable();
        drawable.SetShape(ShapeType.Rectangle);
        drawable.SetColor(fillColor);
        drawable.SetStroke(Dp(1), new Color(strokeColor));
        drawable.SetCornerRadius(Dp(8));
        _modeBadgeCache[(fillColor, strokeColor)] = drawable.GetConstantState();
        return drawable;
    }

    private int ResolveModeBadgeFillColor(string mode, bool lowConfidence)
    {
        var color = GetColor(lowConfidence ? Resource.Color.mode_lb : GetModeColor(mode));
        return Color.Argb(56, color.R, color.G, color.B).ToArgb();
    }

    private int ResolveModeBadgeStrokeColor(string mode, bool lowConfidence)
    {
        var color = GetColor(lowConfidence ? Resource.Color.mode_lb : GetModeColor(mode));
        return Color.Argb(150, color.R, color.G, color.B).ToArgb();
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
        return settings.HasEnabledAlertRule(AlertRuleKind.AnyMessage);
    }

    private static bool IsWatchedCallsignHighlightEnabled(AppSettings settings)
    {
        return settings.HasEnabledAlertRule(AlertRuleKind.WatchedCallsign);
    }

    private static bool IsDxccHighlightEnabled(AppSettings settings)
    {
        return settings.HasEnabledAlertRule(AlertRuleKind.SelectedDxcc);
    }

    private static int GetModeColor(string mode)
    {
        return mode switch
        {
            "FT8" => Resource.Color.mode_ft8,
            "FT4" => Resource.Color.mode_ft4,
            "JT9" => Resource.Color.mode_jt9,
            "Q65" => Resource.Color.mode_q65,
            "WSPR" => Resource.Color.mode_wspr,
            _ => Resource.Color.mode_default
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
            return Resource.Color.match_multi_fill;
        }

        if (matchesWatchedCallsign)
        {
            return Resource.Color.match_callsign_fill;
        }

        if (matchesDxcc)
        {
            return Resource.Color.match_dxcc_fill;
        }

        if (IsAnyMessageHighlightEnabled(settings))
        {
            return Resource.Color.match_any_fill;
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
            PeriodIndicator = root.FindViewById<View>(Resource.Id.periodIndicatorView)!;
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
        public View PeriodIndicator { get; }
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
