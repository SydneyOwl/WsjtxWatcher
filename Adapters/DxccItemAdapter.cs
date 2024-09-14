using Android.Content;
using Android.Views;
using Android.Widget;
using System.Collections.Generic;
using WsjtxWatcher.Database;
using WsjtxWatcher.Variables;
using WsjtxWatcher.ViewModels;

namespace WsjtxWatcher.Adapters;
public class DxccItemAdapter : BaseAdapter<CountryDatabase>
{
    private ListView _listView;
    private List<CountryDatabase> _data;
    private Context _context;

    public DxccItemAdapter(Context context, ListView listView, List<CountryDatabase> data)
    {
        _context = context;
        _listView = listView;
        _data = data;
    }

    public override long GetItemId(int position)
    {
        return position;
    }

    public override CountryDatabase this[int position]
    {
        get { return _data[position]; }
    }

    public override int Count
    {
        get { return _data != null ? _data.Count : 0; }
    }

    public override View GetView(int position, View convertView, ViewGroup parent)
    {
        ViewHolder viewHolder;

        if (convertView == null)
        {
            LayoutInflater inflater = LayoutInflater.From(_context);
            convertView = inflater.Inflate(Resource.Layout.dxcc_item, parent, false);

            viewHolder = new ViewHolder
            {
                DxccDxcc = convertView.FindViewById<TextView>(Resource.Id.dxcc_dxcc),
                DxccMainName = convertView.FindViewById<TextView>(Resource.Id.dxcc_main_name),
                DxccSubName = convertView.FindViewById<TextView>(Resource.Id.dxcc_sub_name),
                DxccItu = convertView.FindViewById<TextView>(Resource.Id.dxcc_itu),
                DxccCq = convertView.FindViewById<TextView>(Resource.Id.dxcc_cq),
                ChDelete = convertView.FindViewById<CheckBox>(Resource.Id.ch_delete)
            };

            convertView.Tag = viewHolder;
        }
        else
        {
            viewHolder = (ViewHolder)convertView.Tag;
        }

        var item = _data[position];
        viewHolder.DxccDxcc.Text = item.DXCC;
        if (SettingsVariables.currentLanguage == "zh")
        {
            viewHolder.DxccMainName.Text = item.CountryNameCN;
            viewHolder.DxccSubName.Text = item.CountryNameEn;
        }
        else
        {
            viewHolder.DxccMainName.Text = item.CountryNameEn;
            viewHolder.DxccSubName.Text = item.CountryNameCN;
        }

        viewHolder.DxccItu.Text = "ITU: "+item.ITUZone.ToString();
        viewHolder.DxccCq.Text = "CQ: "+item.CQZone.ToString();
        viewHolder.ChDelete.Checked = item.Checked;

        _listView.ItemClick += (sender, e) =>
        {
            if (e.Position == position)
            {
                viewHolder.ChDelete.Toggle();
                item.Checked = viewHolder.ChDelete.Checked;
                var sharedPreferences = _context.GetSharedPreferences(_context.GetString(Resource.String.storage_key), FileCreationMode.Private);
                var l = sharedPreferences.GetStringSet("prefered_dxcc",new List<string>()).ToList();
                if (item.Checked)
                {
                    l.Add(item.Id.ToString());
                }
                else
                {
                    l.Remove(item.Id.ToString());
                }
                l = l.Distinct().ToList();
                var edit = sharedPreferences.Edit();
                edit.PutStringSet("prefered_dxcc", l);
                edit.Apply();
            }
        };

        return convertView;
    }

    private class ViewHolder : Java.Lang.Object
    {
        public TextView DxccDxcc { get; set; }
        public TextView DxccMainName { get; set; }
        public TextView DxccSubName { get; set; }
        public TextView DxccItu { get; set; }
        public TextView DxccCq { get; set; }
        public CheckBox ChDelete { get; set; }
    }
}

