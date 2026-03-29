using System.Collections.ObjectModel;
using System.Net;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WsjtxWatcher.Core.Models;

public partial class WatcherState : ObservableObject
{
    private const int MaxMessages = 500;

    [ObservableProperty]
    private string clientId = string.Empty;

    [ObservableProperty]
    private double currentFrequencyHz;

    [ObservableProperty]
    private bool isServiceRunning;

    [ObservableProperty]
    private bool isTimedOut;

    [ObservableProperty]
    private bool isTransmitting;

    [ObservableProperty]
    private bool isWaitingForConnection = true;

    [ObservableProperty]
    private EndPoint? sessionEndPoint;

    [ObservableProperty]
    private int totalMessages;

    [ObservableProperty]
    private int messagesAboutMe;

    [ObservableProperty]
    private string transmitMessage = string.Empty;

    public ObservableCollection<DecodedRadioMessage> Messages { get; } = new();

    public void AddMessage(DecodedRadioMessage message)
    {
        Messages.Add(message);
        if (Messages.Count > MaxMessages)
        {
            Messages.RemoveAt(0);
        }

        TotalMessages += 1;
        if (message.ContainsMyCallsign)
        {
            MessagesAboutMe += 1;
        }
    }

    public void ClearMessages()
    {
        Messages.Clear();
        TotalMessages = 0;
        MessagesAboutMe = 0;
    }

    public void ResetConnection()
    {
        ClientId = string.Empty;
        SessionEndPoint = null;
        CurrentFrequencyHz = 0d;
        IsTransmitting = false;
        TransmitMessage = string.Empty;
        IsWaitingForConnection = true;
        IsTimedOut = false;
    }
}
