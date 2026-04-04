using Android.Content;
using Android.Views;
using Android.Widget;
using AndroidX.RecyclerView.Widget;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.UI.Adapters;

public sealed class IgnoredCallsignAdapter : RecyclerView.Adapter
{
    private readonly Context _context;
    private readonly Func<IgnoredCallsignEntry, Task> _onDeleteRequested;
    private readonly List<IgnoredCallsignEntry> _items = [];

    public IgnoredCallsignAdapter(Context context, Func<IgnoredCallsignEntry, Task> onDeleteRequested)
    {
        _context = context;
        _onDeleteRequested = onDeleteRequested;
        HasStableIds = true;
    }

    public override int ItemCount => _items.Count;

    public void UpdateEntries(IEnumerable<IgnoredCallsignEntry> entries)
    {
        _items.Clear();
        _items.AddRange(entries);
        NotifyDataSetChanged();
    }

    public override long GetItemId(int position)
    {
        var entry = _items[position];
        return HashCode.Combine(
            StringComparer.OrdinalIgnoreCase.GetHashCode(entry.Band),
            StringComparer.OrdinalIgnoreCase.GetHashCode(entry.Callsign));
    }

    public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
    {
        var inflater = LayoutInflater.From(_context) ?? throw new InvalidOperationException("Failed to create layout inflater.");
        var view = inflater.Inflate(Resource.Layout.ignored_callsign_item, parent, false)
            ?? throw new InvalidOperationException("Failed to inflate ignored callsign row.");
        return new ViewHolder(view, _onDeleteRequested);
    }

    public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
    {
        ((ViewHolder)holder).Bind(_items[position], _context);
    }

    private sealed class ViewHolder : RecyclerView.ViewHolder
    {
        private readonly Func<IgnoredCallsignEntry, Task> _onDeleteRequested;
        private IgnoredCallsignEntry? _entry;

        public ViewHolder(View itemView, Func<IgnoredCallsignEntry, Task> onDeleteRequested)
            : base(itemView)
        {
            _onDeleteRequested = onDeleteRequested;
            Text = itemView.FindViewById<TextView>(Resource.Id.ignored_callsign_item_text)!;
            DeleteButton = itemView.FindViewById<Button>(Resource.Id.ignored_callsign_item_delete)!;
            DeleteButton.Click += async (_, _) =>
            {
                var entry = _entry;
                if (entry is null || !DeleteButton.Enabled)
                {
                    return;
                }

                DeleteButton.Enabled = false;
                try
                {
                    await _onDeleteRequested(entry);
                }
                finally
                {
                    if (ReferenceEquals(_entry, entry))
                    {
                        DeleteButton.Enabled = true;
                    }
                }
            };
        }

        public TextView Text { get; }
        public Button DeleteButton { get; }

        public void Bind(IgnoredCallsignEntry entry, Context context)
        {
            _entry = entry;
            Text.Text = context.GetString(
                Resource.String.ignored_callsign_item_format,
                new Java.Lang.String(entry.Band),
                new Java.Lang.String(entry.Callsign));
            DeleteButton.Enabled = true;
        }
    }
}
