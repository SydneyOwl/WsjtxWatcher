using Android.Content;
using Android.Views;
using WsjtxWatcher.Database;
using WsjtxWatcher.Variables;
using Object = Java.Lang.Object;
using System.Collections.Generic;
using System.Linq;
using _Microsoft.Android.Resource.Designer;

namespace WsjtxWatcher.Adapters
{
    public class DxccItemAdapter : BaseAdapter<CountryDatabase>
    {
        private readonly Context _context;
        private readonly List<CountryDatabase> _data;
        private readonly ListView _listView;

        public DxccItemAdapter(Context context, ListView listView, List<CountryDatabase> data)
        {
            _context = context;
            _listView = listView;
            _data = data ?? new List<CountryDatabase>();
        }

        public override CountryDatabase this[int position] => _data[position];

        public override int Count => _data.Count;

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

                // 移除可能存在的旧事件处理器
                viewHolder.ChDelete.Click -= OnCheckBoxClick;
                // 添加新事件处理器
                viewHolder.ChDelete.Click += OnCheckBoxClick;

                convertView.Tag = viewHolder;
            }
            else
            {
                viewHolder = (ViewHolder)convertView.Tag;
            }

            // 确保Tag设置为当前位置
            viewHolder.ChDelete.Tag = position;

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
            
            // 确保复选框状态与数据模型同步
            viewHolder.ChDelete.Checked = item.Checked;

            return convertView;
        }

        private void OnCheckBoxClick(object sender, EventArgs e)
        {
            if (sender is CheckBox checkBox)
            {
                var position = (int)checkBox.Tag;
                if (position >= 0 && position < _data.Count)
                {
                    var item = _data[position];
                    item.Checked = checkBox.Checked;

                    // 保存到SharedPreferences
                    var sharedPreferences = _context.GetSharedPreferences(
                        _context.GetString(ResourceConstant.String.storage_key),
                        FileCreationMode.Private);
                    
                    var preferredDxcc = sharedPreferences
                        .GetStringSet("prefered_dxcc", new List<string>())
                        .ToList();

                    if (item.Checked)
                    {
                        if (!preferredDxcc.Contains(item.Id.ToString()))
                        {
                            preferredDxcc.Add(item.Id.ToString());
                        }
                    }
                    else
                    {
                        preferredDxcc.Remove(item.Id.ToString());
                    }

                    var edit = sharedPreferences.Edit();
                    edit.PutStringSet("prefered_dxcc", preferredDxcc.Distinct().ToList());
                    edit.Apply();
                }
            }
        }

        public void UpdateData(List<CountryDatabase> newData)
        {
            _data.Clear();
            _data.AddRange(newData);
            NotifyDataSetChanged();
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
}