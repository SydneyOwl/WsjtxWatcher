using System.Net.Sockets;
using Android.Content;
using Android.OS;
using DebounceThrottle;

namespace WsjtxWatcher.Utils.DeviceActions;

public class Vibrate
{
    private static ThrottleDispatcher throttleDispatcher = new (TimeSpan.FromMilliseconds(12000));
    public static void DoVibrate(Context ctx)
    {
        throttleDispatcher.Throttle(() =>
        {
            Vibrator vibrator = (Vibrator)ctx.GetSystemService(Context.VibratorService);
            if (vibrator.HasVibrator)
            {
                vibrator.Vibrate(VibrationEffect.CreateOneShot(500, VibrationEffect.DefaultAmplitude));
            }
        });
    }
}