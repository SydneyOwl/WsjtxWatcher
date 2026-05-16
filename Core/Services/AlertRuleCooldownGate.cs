namespace WsjtxWatcher.Core.Services;

public sealed class AlertRuleCooldownGate
{
    private readonly object _sync = new();
    private readonly Dictionary<string, DateTimeOffset> _lastTriggeredAt = new(StringComparer.Ordinal);

    public bool TryEnter(string ruleId, int cooldownSeconds)
    {
        if (cooldownSeconds <= 0)
        {
            return true;
        }

        var now = DateTimeOffset.UtcNow;
        lock (_sync)
        {
            if (_lastTriggeredAt.TryGetValue(ruleId, out var lastTriggeredAt)
                && now - lastTriggeredAt < TimeSpan.FromSeconds(cooldownSeconds))
            {
                return false;
            }

            _lastTriggeredAt[ruleId] = now;
            return true;
        }
    }
}
