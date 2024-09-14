using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Text;
using Java.Lang;
using WsjtxWatcher.Variables;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnCallsignChanged : Object, ITextWatcher
{
    private readonly Context _ctx;

    public OnCallsignChanged(Context ctx)
    {
        this._ctx = ctx;
    }

    public void AfterTextChanged(IEditable? s)
    {
    }

    public void BeforeTextChanged(ICharSequence? s, int start, int count, int after)
    {
    }

    public void OnTextChanged(ICharSequence? s, int start, int before, int count)
    {
        SettingsVariables.MyCallsign = s.ToString().ToUpper();
        // save conf
        var sharedPref =
            _ctx.GetSharedPreferences(_ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        var edit = sharedPref.Edit();
        edit.PutString("callsign", s.ToString().ToUpper());
        edit.Apply();
    }
}