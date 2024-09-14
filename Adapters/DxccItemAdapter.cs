using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Views;
using WsjtxWatcher.Database;
using WsjtxWatcher.Variables;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Adapters;

public class DxccItemAdapter : BaseAdapter<CountryDatabase>
{
    private readonly Context _context;
    private readonly List<CountryDatabase> _data;
    private readonly ListView _listView;

    public DxccItemAdapter(Context context, ListView listView, List<CountryDatabase> data)
    {
        _context = context;
        _listView = listView;
        _data = data;
    }

    public override CountryDatabase this[int position] => _data[position];

    public override int Count => _data != null ? _data.Count : 0;

    public override long GetItemId(int position)
    {
        return position;
    }

    public override View GetView(int position, View convertView, ViewGroup parent)
    {
        ViewHolder viewHolder;

        if (convertView == null)
        {
            var inflater = LayoutInflater.From(_context);
            convertView = inflater.Inflate(ResourceConstant.Layout.dxcc_item, parent, false);

            viewHolder = new ViewHolder
            {
                DxccDxcc = convertView.FindViewById<TextView>(ResourceConstant.Id.dxcc_dxcc),
                DxccMainName = convertView.FindViewById<TextView>(ResourceConstant.Id.dxcc_main_name),
                DxccSubName = convertView.FindViewById<TextView>(ResourceConstant.Id.dxcc_sub_name),
                DxccItu = convertView.FindViewById<TextView>(ResourceConstant.Id.dxcc_itu),
                DxccCq = convertView.FindViewById<TextView>(ResourceConstant.Id.dxcc_cq),
                ChDelete = convertView.FindViewById<CheckBox>(ResourceConstant.Id.ch_delete)
            };

            convertView.Tag = viewHolder;
        }
        else
        {
            viewHolder = (ViewHolder)convertView.Tag;
        }

        var item = _data[position];
        viewHolder.DxccDxcc.Text = item.Dxcc;
        if (SettingsVariables.CurrentLanguage == "zh")
        {
            viewHolder.DxccMainName.Text = item.CountryNameCn;
            viewHolder.DxccSubName.Text = item.CountryNameEn;
        }
        else
        {
            viewHolder.DxccMainName.Text = item.CountryNameEn;
            viewHolder.DxccSubName.Text = item.CountryNameCn;
        }

        viewHolder.DxccItu.Text = "ITU: " + item.ItuZone;
        viewHolder.DxccCq.Text = "CQ: " + item.CqZone;
        viewHolder.ChDelete.Checked = item.Checked;

        _listView.ItemClick += (sender, e) =>
        {
            if (e.Position == position)
            {
                viewHolder.ChDelete.Toggle();
                item.Checked = viewHolder.ChDelete.Checked;
                var sharedPreferences =
                    _context.GetSharedPreferences(_context.GetString(ResourceConstant.String.storage_key),
                        FileCreationMode.Private);
                var l = sharedPreferences.GetStringSet("prefered_dxcc", new List<string>()).ToList();
                if (item.Checked)
                    l.Add(item.Id.ToString());
                else
                    l.Remove(item.Id.ToString());
                l = l.Distinct().ToList();
                var edit = sharedPreferences.Edit();
                edit.PutStringSet("prefered_dxcc", l);
                edit.Apply();
            }
        };

        return convertView;
    }

    private class ViewHolder : Object
    {
        public TextView DxccDxcc { get; set; }
        public TextView DxccMainName { get; set; }
        public TextView DxccSubName { get; set; }
        public TextView DxccItu { get; set; }
        public TextView DxccCq { get; set; }
        public CheckBox ChDelete { get; set; }
    }
}