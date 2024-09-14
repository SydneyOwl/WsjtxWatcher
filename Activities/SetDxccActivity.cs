using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Views;
using Java.Lang;
using WsjtxWatcher.Adapters;
using WsjtxWatcher.Database;

namespace WsjtxWatcher.Activities;

[Activity(Label = "@string/set_dxcc_entity", MainLauncher = false)]
public class SetDxccActivity:Activity
{
    private ListView lv_data;
    private List<CountryDatabase> data;
    private DxccItemAdapter adapter;
    
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        SetContentView(ResourceConstant.Layout.activity_set_dxcc);
        var lv_data = FindViewById<ListView>(ResourceConstant.Id.lv_data);
        var btn_select_all = FindViewById<CheckBox>(ResourceConstant.Id.che_all);
        // 初始化数据
        data = DatabaseHandler.GetInstance(null).QueryAllCountries();
        // 标记checked
        var sharedPreferences = this.GetSharedPreferences(this.GetString(Resource.String.storage_key), FileCreationMode.Private);
        var l = sharedPreferences.GetStringSet("prefered_dxcc",new List<string>()).ToList();
        for (var i = 0; i < data.Count; i++)
        {
            if (l.Contains(data[i].Id.ToString()))
            {
                data[i].Checked = true;
            }
        }
        adapter = new DxccItemAdapter(this, lv_data, data);
        lv_data.Adapter = adapter;
        if (l.Count == data.Count)
        {
            btn_select_all.Checked = true;
        }
        btn_select_all.CheckedChange += (sender, args) =>
        {
            var sharedPreferencesEdit = this.GetSharedPreferences(this.GetString(Resource.String.storage_key), FileCreationMode.Private).Edit();
            var l = new List<string>();
            if (data.Count != 0)
            {
                //判断列表中是否有数据
                if (args.IsChecked)
                {
                    for (int i = 0; i < data.Count; i++)
                    {
                        l.Add(data[i].Id.ToString());
                        data[i].Checked = true;
                    }
                    //通知适配器更新UI
                    adapter.NotifyDataSetChanged();
                }
                else
                {
                    for (int i = 0; i < data.Count; i++)
                    {
                        data[i].Checked = false;
                    }
                    //通知适配器更新UI
                    adapter.NotifyDataSetChanged();
                }

                sharedPreferencesEdit.PutStringSet("prefered_dxcc", l);
                sharedPreferencesEdit.Apply();
            }
            else
            {
                //若列表中没有数据则隐藏全选复选框
                btn_select_all.Visibility = ViewStates.Gone;
            }
        };
    }
}