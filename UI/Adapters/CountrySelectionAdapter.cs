using Android.Content;
using Android.Views;
using Android.Widget;
using WsjtxWatcher.Core.Models;

namespace WsjtxWatcher.UI.Adapters;

public sealed class CountrySelectionAdapter : BaseAdapter<CountrySelectionItem>
{
    private readonly Context _context;
    private readonly IList<CountrySelectionItem> _items;
    private readonly bool _useChineseNames;
    private readonly Func<CountrySelectionItem, bool, Task> _onCheckedChanged;

    public CountrySelectionAdapter(
        Context context,
        IList<CountrySelectionItem> items,
        bool useChineseNames,
        Func<CountrySelectionItem, bool, Task> onCheckedChanged)
    {
        _context = context;
        _items = items;
        _useChineseNames = useChineseNames;
        _onCheckedChanged = onCheckedChanged;
    }

    public override CountrySelectionItem this[int position] => _items[position];

    public override int Count => _items.Count;

    public override long GetItemId(int position)
    {
        return position;
    }

    public override View GetView(int position, View? convertView, ViewGroup? parent)
    {
        var inflater = LayoutInflater.From(_context) ?? throw new InvalidOperationException("Failed to create layout inflater.");
        var view = convertView ?? inflater.Inflate(Resource.Layout.dxcc_item, parent, false)
            ?? throw new InvalidOperationException("Failed to inflate DXCC row.");
        var holder = view.Tag as ViewHolder ?? new ViewHolder(view);
        view.Tag = holder;

        var item = _items[position];
        holder.Dxcc.Text = item.Country.DxccPrefix;
        holder.MainName.Text = _useChineseNames && !string.IsNullOrWhiteSpace(item.Country.ChineseName)
            ? item.Country.ChineseName
            : item.Country.EnglishName;
        holder.SubName.Text = item.Country.EnglishName;
        holder.Itu.Text = $"ITU:{item.Country.ItuZone}";
        holder.Cq.Text = $"CQ:{item.Country.CqZone}";

        if (holder.CheckedChangedHandler is not null)
        {
            holder.CheckBox.CheckedChange -= holder.CheckedChangedHandler;
        }

        if (holder.RowClickHandler is not null)
        {
            view.Click -= holder.RowClickHandler;
        }

        holder.CheckBox.Checked = item.IsSelected;
        holder.CheckedChangedHandler = async (_, args) =>
        {
            await _onCheckedChanged(item, args.IsChecked).ConfigureAwait(false);
        };
        holder.CheckBox.CheckedChange += holder.CheckedChangedHandler;
        holder.RowClickHandler = (_, _) =>
        {
            holder.CheckBox.Checked = !holder.CheckBox.Checked;
        };
        view.Click += holder.RowClickHandler;

        return view;
    }

    private sealed class ViewHolder : Java.Lang.Object
    {
        public ViewHolder(View root)
        {
            Dxcc = root.FindViewById<TextView>(Resource.Id.dxcc_dxcc)!;
            MainName = root.FindViewById<TextView>(Resource.Id.dxcc_main_name)!;
            SubName = root.FindViewById<TextView>(Resource.Id.dxcc_sub_name)!;
            Itu = root.FindViewById<TextView>(Resource.Id.dxcc_itu)!;
            Cq = root.FindViewById<TextView>(Resource.Id.dxcc_cq)!;
            CheckBox = root.FindViewById<CheckBox>(Resource.Id.ch_delete)!;
        }

        public TextView Dxcc { get; }
        public TextView MainName { get; }
        public TextView SubName { get; }
        public TextView Itu { get; }
        public TextView Cq { get; }
        public CheckBox CheckBox { get; }
        public EventHandler<CompoundButton.CheckedChangeEventArgs>? CheckedChangedHandler { get; set; }
        public EventHandler? RowClickHandler { get; set; }
    }
}
