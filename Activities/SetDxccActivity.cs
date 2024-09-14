using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Views;
using WsjtxWatcher.Adapters;
using WsjtxWatcher.Database;

namespace WsjtxWatcher.Activities;

[Activity(Label = "@string/set_dxcc_entity", MainLauncher = false)]
public class SetDxccActivity : Activity
{
    private DxccItemAdapter _adapter;
    private List<CountryDatabase> _data;
    private ListView _lvData;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(ResourceConstant.Layout.activity_set_dxcc);
        var lvData = FindViewById<ListView>(ResourceConstant.Id.lv_data);
        var btnSelectAll = FindViewById<CheckBox>(ResourceConstant.Id.che_all);
        // 初始化数据
        _data = DatabaseHandler.GetInstance(null).QueryAllCountries();
        // 标记checked
        var sharedPreferences =
            GetSharedPreferences(GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        var l = sharedPreferences.GetStringSet("prefered_dxcc", new List<string>()).ToList();
        for (var i = 0; i < _data.Count; i++)
            if (l.Contains(_data[i].Id.ToString()))
                _data[i].Checked = true;
        _adapter = new DxccItemAdapter(this, lvData, _data);
        lvData.Adapter = _adapter;
        if (l.Count == _data.Count) btnSelectAll.Checked = true;
        btnSelectAll.CheckedChange += (sender, args) =>
        {
            var sharedPreferencesEdit =
                GetSharedPreferences(GetString(ResourceConstant.String.storage_key), FileCreationMode.Private).Edit();
            var l = new List<string>();
            if (_data.Count != 0)
            {
                //判断列表中是否有数据
                if (args.IsChecked)
                {
                    for (var i = 0; i < _data.Count; i++)
                    {
                        l.Add(_data[i].Id.ToString());
                        _data[i].Checked = true;
                    }

                    //通知适配器更新UI
                    _adapter.NotifyDataSetChanged();
                }
                else
                {
                    for (var i = 0; i < _data.Count; i++) _data[i].Checked = false;
                    //通知适配器更新UI
                    _adapter.NotifyDataSetChanged();
                }

                sharedPreferencesEdit.PutStringSet("prefered_dxcc", l);
                sharedPreferencesEdit.Apply();
            }
            else
            {
                //若列表中没有数据则隐藏全选复选框
                btnSelectAll.Visibility = ViewStates.Gone;
            }
        };
    }
}