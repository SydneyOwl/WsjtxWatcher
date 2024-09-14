using _Microsoft.Android.Resource.Designer;
using Android.Content;

namespace WsjtxWatcher.Dialogs;

public class ProgDialog
{
    private readonly Context _ctx;
    private readonly AlertDialog.Builder _progressDialog;

    private AlertDialog _dia;

    public ProgDialog(Context ctx)
    {
        this._ctx = ctx;
        _progressDialog = new AlertDialog.Builder(ctx);
    }

    public void StartAni()
    {
        _progressDialog.SetMessage(_ctx.GetString(ResourceConstant.String.loading));
        _progressDialog.SetCancelable(false);
        _progressDialog.SetView(ResourceConstant.Layout.dialog_loading);
        _dia = _progressDialog.Show();
    }

    public void StopAni()
    {
        _dia?.Dismiss();
    }
}