﻿using _Microsoft.Android.Resource.Designer;
using Android.Content;
using Android.Text;
using Java.Lang;
using WsjtxWatcher.Variables;
using Object = Java.Lang.Object;

namespace WsjtxWatcher.Behaviors.Watchers;

public class OnPortChanged : Object, ITextWatcher
{
    private readonly Context ctx;

    public OnPortChanged(Context ctx)
    {
        this.ctx = ctx;
    }

    public void AfterTextChanged(IEditable? s)
    {
    }

    public void BeforeTextChanged(ICharSequence? s, int start, int count, int after)
    {
    }

    public void OnTextChanged(ICharSequence? s, int start, int before, int count)
    {
        SettingsVariables.port = s.ToString();
        // save conf
        var sharedPref =
            ctx.GetSharedPreferences(ctx.GetString(ResourceConstant.String.storage_key), FileCreationMode.Private);
        var edit = sharedPref.Edit();
        edit.PutString("port", s.ToString());
        edit.Apply();
    }
}