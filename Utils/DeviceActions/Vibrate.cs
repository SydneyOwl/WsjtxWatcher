using Android.Content;
using Android.OS;
using DebounceThrottle;

namespace WsjtxWatcher.Utils.DeviceActions;

public class Vibrate
{
    private static readonly ThrottleDispatcher ThrottleDispatcher = new(TimeSpan.FromMilliseconds(12000));

    public static void DoVibrate()
    {
        ThrottleDispatcher.Throttle(() =>
        {
            var vibrator = (Vibrator)Application.Context.GetSystemService(Context.VibratorService);
            if (vibrator.HasVibrator)
                vibrator.Vibrate(VibrationEffect.CreateOneShot(500, VibrationEffect.DefaultAmplitude));
        });
    }
}