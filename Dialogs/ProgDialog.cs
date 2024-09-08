using _Microsoft.Android.Resource.Designer;
using Android.Content;

namespace WsjtxWatcher.Dialogs;

public class ProgDialog
{
    private readonly Context ctx;

    private AlertDialog dia;
    private readonly AlertDialog.Builder progressDialog;

    public ProgDialog(Context ctx)
    {
        this.ctx = ctx;
        progressDialog = new AlertDialog.Builder(ctx);
    }

    public void startAni()
    {
        progressDialog.SetMessage(ctx.GetString(ResourceConstant.String.loading));
        progressDialog.SetCancelable(false);
        progressDialog.SetView(ResourceConstant.Layout.dialog_loading);
        dia = progressDialog.Show();
    }

    public void stopAni()
    {
        dia?.Dismiss();
    }
}