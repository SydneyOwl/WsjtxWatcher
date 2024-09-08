using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WsjtxWatcher.Ft8Transmit;

public class ViewBase
{
    // 监听一下变量变化！
    public event PropertyChangedEventHandler PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    }
}