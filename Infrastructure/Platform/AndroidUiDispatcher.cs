using Android.OS;
using WsjtxWatcher.Core.Contracts;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidUiDispatcher : IUiDispatcher
{
    private readonly Handler _handler = new(Looper.MainLooper!);

    public Task InvokeAsync(Action action)
    {
        if (Looper.MyLooper() == Looper.MainLooper)
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _handler.Post(() =>
        {
            try
            {
                action();
                tcs.SetResult();
            }
            catch (Exception exception)
            {
                tcs.SetException(exception);
            }
        });

        return tcs.Task;
    }
}
