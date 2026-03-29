using Android.App;
using Android.Content;
using Android.OS;
using WsjtxWatcher.Core.Contracts;

namespace WsjtxWatcher.Infrastructure.Platform;

public sealed class AndroidDeviceFeedbackService : IDeviceFeedbackService
{
    private readonly Application _application;
    private DateTimeOffset _lastVibrationAt = DateTimeOffset.MinValue;

    public AndroidDeviceFeedbackService(Application application)
    {
        _application = application;
    }

    public Task VibrateAsync(CancellationToken cancellationToken = default)
    {
        if (DateTimeOffset.UtcNow - _lastVibrationAt < TimeSpan.FromSeconds(12))
        {
            return Task.CompletedTask;
        }

        var vibrator = GetVibrator();
        if (vibrator is null || !vibrator.HasVibrator)
        {
            return Task.CompletedTask;
        }

        _lastVibrationAt = DateTimeOffset.UtcNow;
        vibrator.Vibrate(VibrationEffect.CreateOneShot(500, VibrationEffect.DefaultAmplitude));
        return Task.CompletedTask;
    }

    private Vibrator? GetVibrator()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var vibratorManager = (VibratorManager?)_application.GetSystemService(Context.VibratorManagerService);
            return vibratorManager?.DefaultVibrator;
        }

        return (Vibrator?)_application.GetSystemService(Context.VibratorService);
    }
}
