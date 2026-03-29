namespace WsjtxWatcher.Core.Contracts;

public interface IUiDispatcher
{
    Task InvokeAsync(Action action);
}
